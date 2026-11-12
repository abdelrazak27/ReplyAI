"""Local fake CDP endpoint. Never connects to FiveM, the real chat or a provider."""
import base64
import hashlib
import json
import socket
import struct
import subprocess
import sys
import threading


def exact(connection, length):
    result = b""
    while len(result) < length:
        part = connection.recv(length - len(result))
        if not part:
            raise EOFError
        result += part
    return result


def receive(connection):
    first, second = exact(connection, 2)
    length = second & 127
    if length == 126:
        length = struct.unpack("!H", exact(connection, 2))[0]
    elif length == 127:
        length = struct.unpack("!Q", exact(connection, 8))[0]
    mask = exact(connection, 4) if second & 128 else None
    payload = exact(connection, length)
    if mask:
        payload = bytes(value ^ mask[index % 4] for index, value in enumerate(payload))
    if first & 15 == 8:
        raise EOFError
    return json.loads(payload)


def frame(payload, opcode, final):
    header = bytes([(128 if final else 0) | opcode])
    return header + (bytes([len(payload)]) if len(payload) < 126 else b"\x7e" + struct.pack("!H", len(payload))) + payload


def send(connection, value):
    payload = json.dumps(value).encode()
    middle = len(payload) // 2
    connection.sendall(frame(payload[:middle], 1, False) + frame(payload[middle:], 0, True))


listener = socket.socket()
listener.bind(("127.0.0.1", 0))
listener.listen()
port = listener.getsockname()[1]
failures = []


def serve():
    try:
        while True:
            connection, _ = listener.accept()
            with connection:
                request = b""
                while b"\r\n\r\n" not in request:
                    request += exact(connection, 1)
                headers = request.decode().split("\r\n")
                if headers[0].startswith("GET /json "):
                    payload = json.dumps([{"url": "https://cfx-nui-chat/", "webSocketDebuggerUrl": f"ws://127.0.0.1:{port}/ws"}]).encode()
                    connection.sendall(f"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {len(payload)}\r\nConnection: close\r\n\r\n".encode() + payload)
                    continue
                key = next(line.split(":", 1)[1].strip() for line in headers if line.lower().startswith("sec-websocket-key:"))
                digest = base64.b64encode(hashlib.sha1((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").encode()).digest()).decode()
                connection.sendall(f"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {digest}\r\n\r\n".encode())
                snapshots = 0
                try:
                    while True:
                        command = receive(connection)
                        if command["method"] == "Runtime.enable":
                            send(connection, {"method": "Runtime.executionContextCreated", "params": {"context": {"id": 42}}})
                            send(connection, {"id": command["id"], "result": {}})
                            continue
                        assert command["method"] == "Runtime.evaluate"
                        assert ".chat__messages" in command["params"]["expression"]
                        found = command["params"].get("contextId") == 42
                        lines = []
                        if found:
                            snapshots += 1
                            lines = [{"id": "old", "text": "Old history"}]
                            if snapshots > 1:
                                lines.append({"id": "new-1", "text": "Marcus: Hey, bro!"})
                            if snapshots > 2:
                                lines.append({"id": "new-2", "text": "Marcus: Hey, bro!"})
                        send(connection, {"id": command["id"], "result": {"result": {"value": {"found": found, "lines": lines}}}})
                except (EOFError, ConnectionError):
                    return
    except Exception as error:
        failures.append(error)


thread = threading.Thread(target=serve, daemon=True)
thread.start()
try:
    result = subprocess.run([sys.argv[1], f"http://127.0.0.1:{port}/json"], timeout=15)
    if failures:
        raise failures[0]
    sys.exit(result.returncode)
finally:
    listener.close()
