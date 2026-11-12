import asyncio
import importlib.util
import json
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

spec = importlib.util.spec_from_file_location("translation_engine", Path(__file__).resolve().parents[1] / "src/python_engine.py")
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)


class TranslationTests(unittest.TestCase):
    def request(self, operation="correct", **changes):
        value = {
            "op": operation, "provider": "Groq", "model": "qwen/qwen3.8-27b",
            "key": "FAKE_TEST_KEY_123456", "targetLanguage": "en", "text": "bong jvais revenir",
            "character": {"Name": "Rithy", "Gender": "Masculin", "Context": "Personnage khmer.",
                          "Words": "bong\nlok", "DialogueStyle": "Street US discret", "ActionStyle": "Simple"},
            "shared": "Jyce",
        }
        value.update(changes)
        return value

    def result(self, text):
        return {"text": text, "inputTokens": 10, "outputTokens": 8, "cachedTokens": 0}

    def call(self, request, output):
        generated = AsyncMock(return_value=self.result(output))
        pool = SimpleNamespace(get=AsyncMock(return_value=object()))
        with patch.object(engine, "groq_generate", generated):
            result = asyncio.run(engine.handle_async(request, pool))
        return result, generated

    def test_three_operations_produce_english_in_one_call(self):
        for operation in ("correct", "rephrase", "generate"):
            with self.subTest(operation=operation):
                result, generated = self.call(self.request(operation), "Bong, I'll be back.")
                self.assertTrue(result["ok"])
                self.assertEqual(generated.await_count, 1)
                instructions = generated.await_args.args[2]
                self.assertIn("ENGLISH OUTPUT TAKES PRIORITY", instructions)
                self.assertIn("khmer", instructions)
                self.assertIn("bong", instructions)
                self.assertIn("Street US discret", instructions)

    def test_normal_mode_stays_french(self):
        generated = AsyncMock(return_value=self.result("Bong, j'vais revenir."))
        pool = SimpleNamespace(get=AsyncMock(return_value=object()), preparation_usage={})
        with patch.object(engine, "groq_generate", generated), patch.object(engine, "compact_context", AsyncMock(return_value="Personnage khmer.")):
            asyncio.run(engine.handle_async(self.request(targetLanguage="fr"), pool))
        self.assertNotIn("ENGLISH OUTPUT TAKES PRIORITY", generated.await_args.args[2])

    def test_tense_instructions_reach_all_writing_modes(self):
        for operation, model in (("correct", "qwen/qwen3.8-27b"),
                                 ("correct", "openai/gpt-oss-120b"),
                                 ("rephrase", "openai/gpt-oss-120b"),
                                 ("generate", "openai/gpt-oss-120b")):
            with self.subTest(operation=operation, model=model):
                request = self.request(operation, model=model, targetLanguage="fr",
                                       text="Je fumais quand la porte s’est ouverte.")
                with patch.object(engine, "compact_context", AsyncMock(return_value="")):
                    _, generated = self.call(request, request["text"])
                self.assertIn(engine.KEEP_TENSE, generated.await_args.args[2])

    def test_chat_preserves_ids_and_uses_french_without_character(self):
        lines = [{"id": "a", "text": "Marcus: What's up?"}, {"id": "b", "text": "/me nods."}]
        translated = [{"id": "a", "text": "Marcus : Ça va ?"}, {"id": "b", "text": "/me hoche la tête."}]
        result, generated = self.call(self.request("chat", text=json.dumps(lines), character=None), json.dumps(translated))
        self.assertEqual(json.loads(result["text"]), translated)
        self.assertIn("French", generated.await_args.args[2])
        self.assertNotIn("khmer", generated.await_args.args[2])

    def test_incomplete_or_reordered_chat_is_rejected(self):
        request = self.request("chat", text=json.dumps([{"id": "a", "text": "Hey"}, {"id": "b", "text": "Hey"}]))
        for output in ([], [{"id": "b", "text": "Salut"}, {"id": "a", "text": "Salut"}], [{"id": "a", "text": ""}, {"id": "b", "text": "Salut"}]):
            with self.subTest(output=output), self.assertRaises(ValueError):
                self.call(request, json.dumps(output))

    def test_duplicate_input_ids_rejected_before_api(self):
        generated = AsyncMock()
        with patch.object(engine, "groq_generate", generated), self.assertRaises(ValueError):
            asyncio.run(engine.handle_async(self.request("chat", text=json.dumps([{"id": "a", "text": "Hi"}] * 2)), SimpleNamespace()))
        generated.assert_not_awaited()

    def test_chat_uses_existing_quota_fallback(self):
        request = self.request("chat", text=json.dumps([{"id": "a", "text": "Hi"}]))
        request["connections"] = [
            {"Id": "primary", "Name": "Main", "Provider": "Groq", "Model": request["model"], "Key": request["key"], "QuotaGroup": "one"},
            {"Id": "backup", "Name": "Backup", "Provider": "Groq", "Model": request["model"], "Key": "FAKE_BACKUP_KEY_123456", "QuotaGroup": "two"},
        ]
        generated = AsyncMock(side_effect=[engine.QuotaFailure("quota"), {"ok": True, **self.result('[{"id":"a","text":"Salut"}]')}])
        with patch.object(engine, "handle_one", generated):
            result = asyncio.run(engine.handle_async(request, SimpleNamespace()))
        self.assertEqual(result["attempts"], 2)
        self.assertEqual(result["connectionName"], "Backup")


if __name__ == "__main__":
    unittest.main()
