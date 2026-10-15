import json
import re
import xml.etree.ElementTree as ET
import html
from typing import Any, Dict, List
import boto3


def _text_parts(value):
    """Extract visible assistant text from OpenAI/Bedrock content parts."""
    if isinstance(value, str):
        return value
    if isinstance(value, list):
        return "".join(_text_parts(item) for item in value)
    if isinstance(value, dict):
        if value.get("type") in {"reasoning", "thinking", "reasoningContent", "reasoning_content"}:
            return ""
        if any(key in value for key in ("reasoningContent", "reasoning_content", "reasoning", "thinking")) and not any(
            key in value for key in ("text", "output_text", "content", "final", "answer")
        ):
            return ""
        for key in ("final", "answer"):
            if key in value:
                return _text_parts(value[key])
        for key in ("text", "output_text", "content"):
            if key in value:
                return _text_parts(value[key])
    return ""


def extract_final_assistant_text(payload: Dict[str, Any]) -> str:
    """Parse GPT-OSS responses structurally, excluding reasoning content."""
    choices = payload.get("choices") or []
    if not choices:
        raise RuntimeError("Bedrock response contained no choices")
    choice = choices[0] or {}
    message = choice.get("message") or {}
    # Prefer explicit final/answer fields when a provider supplies them.
    content = message.get("final") or message.get("answer") or message.get("content")
    if not content:
        content = choice.get("final") or choice.get("answer")
    if content is None:
        content = message.get("output_text", choice.get("text", ""))
    # Some Bedrock/OpenAI-compatible responses entity-escape the XML-like tags.
    # Decode once before structural parsing; the final UI escapes user/model text again.
    text = html.unescape(_text_parts(content)).strip()
    # Some GPT-OSS inference responses place XML-like reasoning and final text
    # in one string. Parse a synthetic root first; regex is only malformed-input fallback.
    if "<reasoning" in text.lower():
        try:
            root = ET.fromstring("<response>" + text + "</response>")
            hidden = {"reasoning", "analysis", "thinking"}
            visible_parts = [root.text or ""]
            for node in list(root):
                if str(node.tag).lower() not in hidden:
                    visible_parts.extend(node.itertext())
                # Tail text belongs to the surrounding response, even when the
                # preceding node is a hidden reasoning block.
                visible_parts.append(node.tail or "")
            text = "".join(visible_parts).strip()
        except ET.ParseError:
            text = re.sub(r"<reasoning\b[^>]*>[\s\S]*?</reasoning\s*>", "", text, flags=re.I).strip()
    return text


class BedrockService:
    def __init__(self, settings):
        self.settings = settings
        self.client = boto3.client("bedrock-runtime", region_name=settings.aws_region)

    def embed(self, text: str) -> List[float]:
        body = {"inputText": text, "dimensions": self.settings.embedding_dimensions, "normalize": True}
        response = self.client.invoke_model(
            modelId=self.settings.bedrock_embedding_model_id,
            body=json.dumps(body), contentType="application/json", accept="application/json")
        payload = json.loads(response["body"].read())
        vector = payload.get("embedding") or payload.get("embeddingsByType", {}).get("float")
        if not isinstance(vector, list) or len(vector) != self.settings.embedding_dimensions:
            raise RuntimeError("Bedrock returned an embedding with an unexpected dimension")
        return [float(value) for value in vector]

    def complete(self, system: str, messages: List[Dict[str, str]], max_completion_tokens: int = 180) -> str:
        native_messages = [{"role": item["role"], "content": item["content"]} for item in messages]
        body = {"model": self.settings.bedrock_model_id, "messages": native_messages,
                "max_completion_tokens": max(64, min(700, int(max_completion_tokens))),
                "temperature": 0.2, "top_p": 0.9, "stream": False}
        if system:
            body["messages"].insert(0, {"role": "system", "content": system})
        kwargs: Dict[str, Any] = {"modelId": self.settings.bedrock_model_id,
                                  "body": json.dumps(body), "contentType": "application/json",
                                  "accept": "application/json"}
        if self.settings.bedrock_guardrail_id:
            kwargs["guardrailIdentifier"] = self.settings.bedrock_guardrail_id
            kwargs["guardrailVersion"] = self.settings.bedrock_guardrail_version
        response = self.client.invoke_model(**kwargs)
        payload = json.loads(response["body"].read())
        choices = payload.get("choices") or []
        if not choices:
            raise RuntimeError("Bedrock response contained no choices")
        content = extract_final_assistant_text(payload)
        if not content:
            raise RuntimeError("Bedrock response contained no assistant text")
        return content
