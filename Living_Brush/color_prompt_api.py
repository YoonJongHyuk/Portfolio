from fastapi import FastAPI
from pydantic import BaseModel
import ollama
import re

app = FastAPI()

class TagRequest(BaseModel):
    tag: str

# 🎯 미리 정해둔 색상 팔레트 (MVP용)
PRESET_TAG_COLORS = {
    "민화풍": [
        "#C41E3A", "#F5C842", "#5E503F", "#A7C6ED", "#E0B0FF",
        "#846C5B", "#ACD8AA", "#D46A6A", "#FFF1C1", "#564138"
    ],
    "고흐풍": [
        "#002F6C", "#F4C542", "#FFD700", "#F9A602", "#004E89",
        "#A2BCE0", "#5B5EA6", "#F88379", "#F6E7D7", "#1A1A40"
    ],
    "모네풍": [
        "#B4D4D3", "#F7EFE2", "#FFE8D6", "#D6D2C4", "#B7B8B6",
        "#A3C6C4", "#D8B4A0", "#C6DBDA", "#AFCBCE", "#EAE3DC"
    ]
}

@app.post("/generate-colors-by-tag")
async def generate_colors(data: TagRequest):
    tag = data.tag.strip()

    # ✅ 1. 미리 정의된 태그라면 LLM 호출 생략
    if tag in PRESET_TAG_COLORS:
        return {"hex_list": PRESET_TAG_COLORS[tag]}

    # ✅ 2. 그 외 태그는 LLM 호출
    prompt = f"""
당신은 색채 이론과 예술사에 정통한 색채 전문가입니다.
'{tag}' 스타일에 어울리는 대표적인 색상 10가지를 HEX 코드로 추천해 주세요.
선택된 색상들은 해당 태그의 시각적 아이덴티티를 명확히 나타내야 합니다.

요구 사항:
- 각 색상은 '{tag}' 특유의 분위기와 시각적 정체성을 잘 표현해야 합니다.
- 예술적 연상, 시대적 분위기, 색조의 조화 등을 고려하여 선택하세요.
- 결과는 쉼표(,)로 구분된 HEX 코드 10개만 출력하세요.
- 예시: #C41E3A, #F5C842, ...
"""

    response = ollama.chat(
        model="hyperclovax-1.5b-q8-mtvs:latest",
        messages=[
            {"role": "user", "content": prompt}
        ]
    )

    text = response['message']['content']
    hex_list = re.findall(r"#(?:[0-9a-fA-F]{6})", text)

    return {"hex_list": hex_list}


# uvicorn color_prompt_api:app --reload --port 8080