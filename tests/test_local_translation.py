"""Real model smoke/latency check. No Groq or network required."""
import json
from pathlib import Path
import subprocess
import sys
import time

worker = Path(sys.argv[1]).resolve()
lines = [
    "Marcus Hill says: Keep your hands where I can see them.",
    "/do The smell of marijuana fills the room.",
    "*takes a drag from his cigarette* We need to leave, bro.",
    "John Doe: I burned the car. Nobody saw me.",
    "Get the fuck out of here!",
    "She takes off her clothes and kisses her girlfriend.",
]
process = subprocess.Popen([str(worker)], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
try:
    for iteration in range(2):
        started = time.perf_counter()
        payload = {"lines": [{"id": str(i), "text": text} for i, text in enumerate(lines)]}
        process.stdin.write((json.dumps(payload) + "\n").encode()); process.stdin.flush()
        # communicate/timeout is handled by the parent test runner for the persistent protocol.
        reply = json.loads(process.stdout.readline())
        assert reply["ok"], reply
        assert [x["id"] for x in reply["lines"]] == list(map(str, range(6)))
        output = [x["text"] for x in reply["lines"]]
        assert all(x.strip() for x in output)
        assert output[0].startswith("Marcus Hill dit : ")
        assert output[1].startswith("/do ")
        assert output[2].count("*") == 2
        assert output[3].startswith("John Doe : ")
        print(json.dumps({"seconds": round(time.perf_counter()-started, 3), "translations": output}, ensure_ascii=False))
    process.stdin.write(b'{"lines":[]}\n'); process.stdin.flush()
    assert json.loads(process.stdout.readline())["ok"] is False
    print("PASS persistent offline worker, IDs, delimiters, speaker names, invalid input")
finally:
    process.kill(); process.wait(timeout=5)
