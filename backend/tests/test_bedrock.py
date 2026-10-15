import json
from app.bedrock import BedrockService, extract_final_assistant_text
from app.config import Settings


class FakeBody:
    def __init__(self, payload): self.payload = payload
    def read(self): return json.dumps(self.payload).encode("utf-8")


class FakeClient:
    def __init__(self): self.calls = []
    def invoke_model(self, **kwargs):
        self.calls.append(kwargs)
        if "embed" in kwargs["modelId"]:
            return {"body": FakeBody({"embedding": [0.0] * 1024})}
        return {"body": FakeBody({"choices": [{"message": {"content": "Use clean water."}}]})}


def test_native_payloads_and_guardrail():
    service = object.__new__(BedrockService)
    service.settings = Settings(BEDROCK_GUARDRAIL_ID="guard", BEDROCK_GUARDRAIL_VERSION="1")
    service.client = FakeClient()
    assert len(service.embed("water")) == 1024
    assert service.complete("guide", [{"role": "user", "content": "What should I drink?"}]) == "Use clean water."
    embed = json.loads(service.client.calls[0]["body"])
    completion = json.loads(service.client.calls[1]["body"])
    assert embed["inputText"] == "water" and embed["dimensions"] == 1024
    assert completion["messages"][0]["role"] == "system"
    assert service.client.calls[1]["guardrailIdentifier"] == "guard"


def test_reasoning_is_removed_structurally_and_final_text_kept():
    payload = {"choices": [{"message": {"content": [
        {"type": "reasoning", "text": "hidden chain"},
        {"type": "text", "text": "Drink boiled water."}
    ]}}]}
    assert extract_final_assistant_text(payload) == "Drink boiled water."
    tagged = {"choices": [{"message": {"content": "<reasoning>hidden</reasoning>\nDrink rainwater."}}]}
    assert extract_final_assistant_text(tagged) == "Drink rainwater."


def test_entity_escaped_reasoning_is_removed_before_ui():
    tagged = {"choices": [{"message": {"content": "&lt;reasoning&gt;hidden&lt;/reasoning&gt;Drink rainwater."}}]}
    assert extract_final_assistant_text(tagged) == "Drink rainwater."


def test_separate_reasoning_content_block_is_excluded():
    tagged = {"choices": [{"message": {"content": [
        {"reasoningContent": {"reasoningText": {"text": "hidden"}}},
        {"text": "Drink rainwater."},
    ]}}]}
    assert extract_final_assistant_text(tagged) == "Drink rainwater."
