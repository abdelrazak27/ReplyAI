import asyncio
import json
import inspect
from unittest.mock import patch, AsyncMock
import importlib.util
import unittest
from pathlib import Path
from types import SimpleNamespace as NS

path = Path(__file__).resolve().parents[1] / "src" / "python_engine.py"
spec = importlib.util.spec_from_file_location("parole_python", path)
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)


def chunk(text=None, finish=None, thought=False, usage=None, call=None):
    delta=NS(content=None if thought else text,reasoning=text if thought else None,tool_calls=call)
    return NS(choices=[NS(index=0,finish_reason=finish,delta=delta)],usage=usage,x_groq=None)


def fake_client(chunks):
    class Stream:
        def __init__(self):self.values=iter(chunks)
        def __aiter__(self):return self
        async def __anext__(self):
            try:return next(self.values)
            except StopIteration:raise StopAsyncIteration
        async def close(self):pass
    class Completions:
        async def create(self,**kwargs):return Stream()
    return NS(chat=NS(completions=Completions()))


def run_stream(chunks):
    return asyncio.run(engine.groq_generate(fake_client(chunks),'qwen/qwen3.8-27b','instructions','message',128))


class EngineTests(unittest.TestCase):
    def test_stream_collects_parts_and_final_usage(self):
        result = run_stream([chunk("Frère, "), chunk("calme-toi.", "stop",
            usage=NS(prompt_tokens=12, completion_tokens=8,prompt_tokens_details=None))])
        self.assertEqual(result, {"text": "Frère, calme-toi.", "inputTokens": 12, "outputTokens": 8,"cachedTokens":0})

    def test_thought_is_never_returned(self):
        result = run_stream([chunk("raisonnement privé", thought=True), chunk("Bonjour.", "stop")])
        self.assertEqual(result["text"], "Bonjour.")

    def test_partial_stream_is_rejected(self):
        with self.assertRaises(ValueError):
            run_stream([chunk("Réponse incomplète")])

    def test_max_tokens_is_rejected(self):
        with self.assertRaises(ValueError):
            run_stream([chunk("Début", "length")])

    def test_empty_answer_is_rejected(self):
        with self.assertRaises(ValueError):
            run_stream([chunk(" ", "stop")])

    def test_tool_call_is_not_a_correction(self):
        with self.assertRaises(ValueError):
            run_stream([chunk("", "stop", call=NS(name="search"))])

    def test_long_answer_rejected(self):
        with self.assertRaises(ValueError):
            run_stream([chunk("x" * 16001, "stop")])

    def test_profile_and_selection_are_json_data(self):
        prompt = engine.prompt_for({"Name": 'Rithy "Ree"', "Gender": "Masculin", "Context": "Contexte", "Words": "bong\nlok"}, "homie\n", "*b-bro...* jvais check")
        self.assertIn('Rithy \\"Ree\\"'.replace('\\\\', '\\'), prompt)
        self.assertIn("N'ajoute pas le 'ne'", prompt)
        self.assertIn("bong", prompt)
        self.assertIn("MESSAGE=\"*b-bro...* jvais check\"", prompt)

    def test_vocabulary_limits_and_duplicates(self):
        self.assertEqual(engine.vocabulary("homie\nHOMIE\n# note\nlok"), ["homie", "lok"])
        with self.assertRaises(ValueError):
            engine.vocabulary("\n".join(str(i) for i in range(251)))

    def test_sdk_arguments_are_explicit(self):
        calls=[]
        class Completions:
            async def create(self,**kwargs):
                calls.append(kwargs)
                return await fake_client([chunk('Bonjour frère.','stop')]).chat.completions.create()
        class Pool:
            async def get(self,*args):return NS(chat=NS(completions=Completions()))
        result=asyncio.run(engine.handle_async({'op':'test','key':'FAKE_TEST_KEY_123456','model':'qwen/qwen3.8-27b'},Pool()))
        self.assertTrue(result['ok'])
        self.assertEqual(calls[0]['temperature'],0.0)
        self.assertEqual(calls[0]['reasoning_effort'],'none')
        self.assertNotIn('tools',calls[0])
        self.assertEqual(len(calls),1)

    def test_invalid_request_does_not_create_client(self):
        def forbidden(**kwargs): raise AssertionError("Réseau déclenché")
        with self.assertRaisesRegex(ValueError, "Identifiant de modèle invalide"):
            asyncio.run(engine.handle_async({"op": "correct", "key": "FAKE_TEST_KEY_123456", "model": "../bad"}, forbidden))

    def test_french_source_encoding(self):
        self.assertIn("français", engine.prompt_for({}, "", "bonjour"))
        self.assertNotIn("Ã", engine.prompt_for({}, "", "bonjour"))

    def test_client_is_reused_and_sdk_retries_are_disabled(self):
        async def run():
            client = NS()
            with patch.object(engine, "AsyncGroq", return_value=client) as factory:
                pool = engine.ClientPool()
                self.assertIs(await pool.get("Groq", "FAKE_TEST_KEY_123456"), client)
                self.assertIs(await pool.get("Groq", "FAKE_TEST_KEY_123456"), client)
                self.assertEqual(factory.call_count, 1)
                self.assertEqual(factory.call_args.kwargs["max_retries"], 0)
                self.assertEqual(factory.call_args.kwargs["timeout"], 10.0)
        asyncio.run(run())

    def test_retired_provider_is_rejected_before_network(self):
        pool=NS(get=AsyncMock())
        with self.assertRaises(ValueError):
            asyncio.run(engine.handle_async({'op':'test','provider':'Google','key':'FAKE_TEST_KEY_123456','model':'retired-model'},pool))
        pool.get.assert_not_awaited()

    def test_global_deadline_cancels_even_a_silent_provider(self):
        class Pool:
            async def get(self, *args):
                await asyncio.sleep(10)
        async def run():
            start = asyncio.get_running_loop().time()
            with self.assertRaises(TimeoutError):
                with patch.object(engine,"TIMEOUT_SECONDS",0.05):
                    await engine.handle_async({"op":"test", "key":"FAKE_TEST_KEY_123456", "model":"qwen/qwen3.8-27b"}, Pool())
            self.assertLess(asyncio.get_running_loop().time()-start, 0.3)
            self.assertEqual(engine.TIMEOUT_SECONDS,10.0)
        asyncio.run(run())

    def test_answer_after_two_seconds_is_accepted_before_ten_seconds(self):
        async def generate(*args):
            await asyncio.sleep(2.05)
            return {'text':'Bonjour frère.','inputTokens':5,'outputTokens':3}
        class Pool:
            async def get(self,*args):return None
        with patch.object(engine,'groq_generate',generate):
            result=asyncio.run(engine.handle_async({'op':'test','key':'FAKE_TEST_KEY_123456','model':'qwen/qwen3.8-27b'},Pool()))
        self.assertTrue(result['ok'])
        self.assertGreaterEqual(result['milliseconds'],2000)
        self.assertLess(result['milliseconds'],10000)

    def test_provider_errors_recognize_groq_status(self):
        class Failure(Exception): status_code=429
        self.assertEqual(engine.safe_failure(Failure("Quota"), "")['code'], 429)

    def test_compact_context_is_generated_once_and_invalidated_on_edit(self):
        async def run():
            pool=engine.ClientPool()
            response={'text':'Khmer, bouddhiste.', 'inputTokens':150, 'outputTokens':10}
            with patch.object(engine,'groq_generate',new_callable=AsyncMock,return_value=response) as generate:
                context='Une histoire de personnage. '*20
                self.assertEqual(await engine.compact_context(None,context,pool),'Khmer, bouddhiste.')
                await engine.compact_context(None,context,pool)
                self.assertEqual(generate.call_count,1)
                await engine.compact_context(None,context+'Nouvelle donnée.',pool)
                self.assertEqual(generate.call_count,2)
        asyncio.run(run())

    def test_short_context_does_not_require_a_summary_call(self):
        pool=engine.ClientPool()
        self.assertEqual(asyncio.run(engine.compact_context(None,'Khmer bouddhiste.',pool)),'Khmer bouddhiste.')

    def test_compact_prompt_keeps_identity_gender_and_dictionary(self):
        value=engine.compact_system({'Name':'Sophea','Gender':'Féminin','Words':'bong\nlok'},'homie','Khmer bouddhiste.')
        for expected in ['Sophea','Féminin','bong','lok','homie','Khmer bouddhiste.']:
            self.assertIn(expected,value)

    def test_relevant_lexicon_includes_exact_words_and_near_miss_names(self):
        entries=['homie','bong','handshake','Jyce','J']
        selected=engine.relevant_words(entries,"homie un hanchasse pour Jyec")
        self.assertIn('homie',selected)
        self.assertIn('handshake',selected)
        self.assertIn('Jyce',selected)
        self.assertNotIn('J',engine.relevant_words(entries,"j'vais bien"))
        self.assertEqual(entries,['homie','bong','handshake','Jyce','J'])

    def test_groq_keeps_system_prefix_static_and_reasoning_out_of_text(self):
        requests=[]
        class Stream:
            def __init__(self): self.done=False
            def __aiter__(self): return self
            async def __anext__(self):
                if self.done: raise StopAsyncIteration
                self.done=True
                delta=NS(content="Bonjour frère.", reasoning="never copy this", tool_calls=None)
                usage=NS(prompt_tokens=100,completion_tokens=12,prompt_tokens_details=NS(cached_tokens=80))
                return NS(choices=[NS(index=0,finish_reason="stop",delta=delta)], usage=usage,x_groq=None)
            async def close(self):pass
        class Completions:
            async def create(self,**kwargs):requests.append(kwargs);return Stream()
        client=NS(chat=NS(completions=Completions()))
        result=asyncio.run(engine.groq_generate(client,"openai/gpt-oss-120b","fixed profile","bonjour frere",128))
        self.assertEqual(result['text'],"Bonjour frère.")
        self.assertEqual(result['cachedTokens'],80)
        self.assertEqual(requests[0]['messages'][0],{'role':'system','content':'fixed profile'})
        self.assertEqual(requests[0]['reasoning_effort'],'low')
        self.assertFalse(requests[0]['include_reasoning'])
        async def validate_signature():
            client=engine.AsyncGroq(api_key="FAKE_TEST_KEY_123456")
            try:
                inspect.signature(client.chat.completions.create).bind_partial(**requests[0])
            finally:
                await client.close()
        asyncio.run(validate_signature())


    def test_secret_is_redacted(self):
        class Failure(Exception):
            code = 500
            message = "Internal FAKE_TEST_KEY_123456 https://example.test?key=private"
        result = engine.safe_failure(Failure(), "FAKE_TEST_KEY_123456")
        self.assertEqual(result["code"], 500)
        self.assertNotIn("FAKE_TEST_KEY_123456", result["message"])
        self.assertNotIn("private", result["message"])

    def test_programming_error_is_not_reported_as_a_network_error(self):
        result=engine.safe_failure(TypeError("unsupported argument"),"")
        self.assertIn("Erreur locale",result['message'])
        self.assertIn("TypeError",result['message'])
        self.assertNotIn("Connexion",result['message'])

    def test_invalid_key_type_can_be_reported(self):
        self.assertFalse(engine.safe_failure(ValueError("Clé invalide"), 123)["ok"])


class FallbackTests(unittest.TestCase):
    def connection(self, provider="Groq", key="FAKE_FIRST_KEY_123456", group="principal", name="Principale"):
        return {"Provider":provider,"Model":"qwen/qwen3.8-27b", "Key":key,"QuotaGroup":group,"Name":name}

    def request(self, *connections):
        return {"op":"correct","text":"non t qui toi ?","character":{"Name":"Test","Context":""},"connections":list(connections)}

    def failure(self, code, retry=None):
        error=RuntimeError("Provider failure")
        error.status_code=code
        error.response=NS(headers={} if retry is None else {"retry-after":str(retry)})
        return error

    def test_success_does_not_contact_any_fallback(self):
        generate=AsyncMock(return_value={"ok":True,"text":"Non, t'es qui, toi ?"})
        with patch.object(engine,"handle_one",generate):
            result=asyncio.run(engine.handle_async(self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2")),NS()))
        self.assertEqual(generate.await_count,1)
        self.assertEqual(result['providerUsed'],'Groq')
        self.assertEqual(result['attempts'],1)

    def test_quota_replays_same_selection_and_profile_on_second_provider(self):
        generate=AsyncMock(side_effect=[self.failure(429),{"ok":True,"text":"Non, t'es qui, toi ?"}])
        request=self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2"))
        with patch.object(engine,"handle_one",generate):
            result=asyncio.run(engine.handle_async(request,NS()))
        calls=generate.await_args_list
        self.assertEqual(calls[0].args[0]['text'],calls[1].args[0]['text'])
        self.assertEqual(calls[0].args[0]['character'],calls[1].args[0]['character'])
        self.assertNotIn('connections',calls[1].args[0])
        self.assertEqual(result['connectionName'],'Secours Groq 2')
        self.assertEqual(result['attempts'],2)

    def test_shared_quota_keys_are_skipped_and_not_counted_as_extra_capacity(self):
        generate=AsyncMock(side_effect=[self.failure(429),{"ok":True,"text":"OK"}])
        request=self.request(self.connection(),self.connection(key="FAKE_OTHER_KEY_123456"),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2"))
        with patch.object(engine,"handle_one",generate):
            result=asyncio.run(engine.handle_async(request,NS()))
        self.assertEqual(generate.await_count,2)
        self.assertEqual(result['providerUsed'],'Groq')

    def test_identical_key_is_not_retried_under_another_group_label(self):
        generate=AsyncMock(side_effect=[self.failure(429),{"ok":True,"text":"OK"}])
        request=self.request(self.connection(),self.connection(group="another"),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2"))
        with patch.object(engine,"handle_one",generate):
            asyncio.run(engine.handle_async(request,NS()))
        self.assertEqual(generate.await_count,2)
        self.assertEqual(generate.await_args_list[1].args[0]['key'],'FAKE_SECOND_KEY_123456')

    def test_key_network_server_and_content_errors_never_trigger_fallback(self):
        for error in [self.failure(401),self.failure(403),self.failure(500),ConnectionError("offline"),ValueError("partial response")]:
            with self.subTest(error=error):
                generate=AsyncMock(side_effect=error)
                with patch.object(engine,"handle_one",generate),self.assertRaises(type(error)):
                    asyncio.run(engine.handle_async(self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2")),NS()))
                self.assertEqual(generate.await_count,1)

    def test_retry_after_is_remembered_across_corrections(self):
        pool=NS();generate=AsyncMock(side_effect=[self.failure(429,120),{"ok":True,"text":"OK"},{"ok":True,"text":"OK again"}])
        request=self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2"))
        with patch.object(engine,"handle_one",generate):
            asyncio.run(engine.handle_async(request,pool))
            asyncio.run(engine.handle_async(request,pool))
        self.assertEqual(generate.await_count,3)
        self.assertEqual(generate.await_args_list[2].args[0]['key'],'FAKE_SECOND_KEY_123456')
        self.assertGreater(pool.quota_cooldowns[engine.quota_scope(self.connection())],engine.time.monotonic()+100)

    def test_all_quotas_exhausted_does_not_loop(self):
        pool=NS();generate=AsyncMock(side_effect=self.failure(429))
        request=self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2"))
        with patch.object(engine,"handle_one",generate):
            with self.assertRaises(engine.QuotaFailure):asyncio.run(engine.handle_async(request,pool))
            with self.assertRaises(engine.QuotaFailure):asyncio.run(engine.handle_async(request,pool))
        self.assertEqual(generate.await_count,2)

    def test_entire_chain_has_one_deadline_including_failed_attempt(self):
        calls=[]
        async def generate(request,pool):
            calls.append(request['key'])
            await asyncio.sleep(.03)
            if len(calls)==1:raise self.failure(429)
            return {"ok":True,"text":"must not be pasted"}
        with patch.object(engine,"TIMEOUT_SECONDS",.05),patch.object(engine,"handle_one",generate),self.assertRaises(TimeoutError):
            asyncio.run(engine.handle_async(self.request(self.connection(),self.connection(key="FAKE_SECOND_KEY_123456",group="second",name="Secours Groq 2")),NS()))
        self.assertEqual(calls,['FAKE_FIRST_KEY_123456','FAKE_SECOND_KEY_123456'])

    def test_disabled_fallback_legacy_request_still_works(self):
        generate=AsyncMock(return_value={"ok":True,"text":"Bonjour."})
        with patch.object(engine,"handle_one",generate):
            asyncio.run(engine.handle_async({"op":"correct","text":"bonjour"},NS()))
        self.assertEqual(generate.await_count,1)

    def test_invalid_route_is_rejected_before_any_network_call(self):
        generate=AsyncMock()
        bad=self.connection();bad['Key']='bad'
        with patch.object(engine,"handle_one",generate),self.assertRaises(ValueError):
            asyncio.run(engine.handle_async(self.request(self.connection(),bad),NS()))
        self.assertEqual(generate.await_count,0)


class RephraseTests(unittest.TestCase):
    def request(self):
        return {"op":"rephrase", "provider":"Groq", "model":"openai/gpt-oss-120b", "key":"FAKE_TEST_KEY_123456",
                "character":{"Name":"Ree", "Context":"Khmer", "Gender":"Masculin", "DialogueStyle":"Street US discret", "ActionStyle":"Descriptif", "ActionInstructions":"Précis."},
                "shared":"bong", "text":"bong *baisse les yeux* on bouge ?"}

    def test_rephrase_uses_one_call_and_independent_styles(self):
        request=self.request()
        pool=NS(get=AsyncMock(return_value=object()))
        generate=AsyncMock(return_value={"text":"bong *abaisse le regard* on y va ?", "inputTokens":100,"outputTokens":20,"cachedTokens":0})
        with patch.object(engine,"groq_generate",generate),patch.object(engine,"compact_context",AsyncMock()) as prepare:
            result=asyncio.run(engine.handle_async(request,pool))
        self.assertEqual(result['text'],generate.return_value['text'])
        self.assertEqual(generate.await_count,1)
        self.assertEqual(generate.await_args.kwargs["temperature"],0.0)
        self.assertEqual(generate.await_args.args[4],3072)
        self.assertEqual(generate.await_args.kwargs["reasoning_effort"],"medium")
        self.assertEqual(json.loads(generate.await_args.args[3])["texte"],request["text"])
        self.assertEqual(prepare.await_count,0)
        system=generate.await_args.args[2]
        self.assertIn("Français majoritaire",system)
        self.assertIn("description concrète",system)
        self.assertIn("/ame",system)
        self.assertIn("/do décrit un fait",system)
        self.assertIn('"actions": "Précis."',system)
        self.assertIn("bong",system)

    def test_unknown_style_rejected_before_network(self):
        request=self.request();request['character']['ActionStyle']='inconnu'
        pool=NS(get=AsyncMock())
        with self.assertRaises(ValueError):asyncio.run(engine.handle_async(request,pool))
        pool.get.assert_not_called()

    def test_rephrase_quota_fallback_preserves_operation_and_styles(self):
        request=self.request()
        request['connections']=[{"Provider":"Groq","Model":request['model'],"Key":"FAKE_FIRST_KEY_123456","QuotaGroup":"first","Name":"First"},
                                {"Provider":"Groq","Model":request['model'],"Key":"FAKE_SECOND_KEY_123456","QuotaGroup":"second","Name":"Second"}]
        error=RuntimeError('quota');error.status_code=429
        generate=AsyncMock(side_effect=[error,{"ok":True,"text":"résultat"}])
        with patch.object(engine,"handle_one",generate):result=asyncio.run(engine.handle_async(request,NS()))
        self.assertEqual(result['attempts'],2)
        for call in generate.await_args_list:
            self.assertEqual(call.args[0]['op'],'rephrase')
            self.assertEqual(call.args[0]['character'],request['character'])

    def test_rephrase_whole_chain_keeps_ten_second_budget(self):
        request=self.request();request['connections']=[{"Provider":"Groq","Model":request['model'],"Key":request['key'],"QuotaGroup":"first","Name":"First"}]
        async def slow(*args):
            await asyncio.sleep(.08)
            return {"ok":True,"text":"trop tard"}
        with patch.object(engine,"TIMEOUT_SECONDS",.03),patch.object(engine,"handle_one",slow),self.assertRaises(TimeoutError):
            asyncio.run(engine.handle_async(request,NS()))


class GenerationTests(unittest.TestCase):
    def request(self):
        return {"op":"generate","provider":"Groq","model":"openai/gpt-oss-120b","key":"FAKE_TEST_KEY_123456",
                "character":{"Name":"Test","Gender":"Féminin","Context":"Khmer","ActionStyle":"Cinématographique","GenerationFormat":"/do","GenerationDetail":"Détaillé","GenerationInstructions":"Odeur seulement."},
                "shared":"lok", "text":"une odeur car j'ai brûlé la voiture"}

    def test_generation_one_call_uses_settings_and_json(self):
        request=self.request();pool=NS(get=AsyncMock(return_value=object()))
        generate=AsyncMock(return_value={"text":"/do Une odeur âcre flotte.","inputTokens":120,"outputTokens":40,"cachedTokens":0})
        with patch.object(engine,"groq_generate",generate):result=asyncio.run(engine.handle_async(request,pool))
        self.assertTrue(result['ok']);self.assertEqual(generate.await_count,1)
        system=generate.await_args.args[2]
        self.assertIn("FORMAT=/do",system);self.assertIn("35 à 60 mots",system)
        self.assertIn("Odeur seulement.",system);self.assertIn("Féminin",system)
        self.assertEqual(json.loads(generate.await_args.args[3])['texte'],request['text'])
        self.assertEqual(generate.await_args.kwargs['reasoning_effort'],'medium')

    def test_invalid_generation_config_rejected_before_network(self):
        for field,value in [('GenerationFormat','/quit'),('GenerationDetail','inconnu'),('GenerationInstructions','x'*1001)]:
            request=self.request();request['character'][field]=value;pool=NS(get=AsyncMock())
            with self.assertRaises(ValueError):asyncio.run(engine.handle_async(request,pool))
            pool.get.assert_not_called()

    def test_generation_fallback_keeps_idea_and_operation(self):
        request=self.request();request['connections']=[{'Provider':'Groq','Model':request['model'],'Key':'FAKE_FIRST_KEY_123456','QuotaGroup':'first','Name':'First'}, {'Provider':'Groq','Model':request['model'],'Key':'FAKE_SECOND_KEY_123456','QuotaGroup':'second','Name':'Second'}]
        error=RuntimeError('quota');error.status_code=429
        generate=AsyncMock(side_effect=[error,{'ok':True,'text':'/do Une odeur flotte.'}])
        with patch.object(engine,'handle_one',generate):result=asyncio.run(engine.handle_async(request,NS()))
        self.assertEqual(result['attempts'],2)
        for call in generate.await_args_list:
            self.assertEqual(call.args[0]['op'],'generate');self.assertEqual(call.args[0]['text'],request['text'])
            self.assertEqual(call.args[0]['character'],request['character'])

    def test_generation_only_selects_relevant_profile_references(self):
        context="Il appartient au gang SPC. Sa grand-mère est morte. Il appelle ses proches bong en référence à ses origines khmères."
        self.assertEqual(engine.generation_context(context,"Une lumière rouge clignote au-dessus de la porte."),"")
        self.assertIn("bong",engine.generation_context(context,"Je tends un objet à bong."))
        self.assertNotIn("grand-mère",engine.generation_context(context,"Je tends un objet à bong."))

    def test_generation_has_same_total_deadline(self):
        request=self.request()
        async def slow(*args,**kwargs):await asyncio.sleep(.08);return {'text':'/me trop tard.'}
        with patch.object(engine,'TIMEOUT_SECONDS',.03),patch.object(engine,'groq_generate',slow),self.assertRaises(TimeoutError):
            asyncio.run(engine.handle_async(request,NS(get=AsyncMock(return_value=object()))))


if __name__ == "__main__":
    unittest.main()
