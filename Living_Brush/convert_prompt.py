import os
os.environ["CUDA_VISIBLE_DEVICES"] = "0"

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import pandas as pd
from rag_chain import create_rag_chain
import ollama
import re
import json
from urllib import request
import asyncio
from rapidfuzz import fuzz
from typing import Optional
import json
from pathlib import Path
import httpx
import unicodedata
from collections import defaultdict
import requests

def load_local_env(env_path: str = ".env") -> None:
    env_file = Path(env_path)
    if not env_file.exists():
        return

    for raw_line in env_file.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        if key and key not in os.environ:
            os.environ[key] = value


load_local_env()




COMFYUI_IP = os.getenv("COMFYUI_IP", "127.0.0.1:9000")


# 대구에서 로컬로 돌릴때 쓰는 ip
#COMFYUI_IP = "127.0.0.1:9000"

# CUDA_VISIBLE_DEVICES=0 python main.py --port 9000 --listen 0.0.0.0


app = FastAPI()
qa_chain = create_rag_chain()

@app.get("/")
async def root_index():
    return "Text Success"

model="llama3:8b"

#------------------ AI 브러시 -----------------------

# 고정 클래스 목록 (기존 그대로 사용)
BRUSH_CLASSES = [
    "Coarse Bristles", "Duct Tape", "Fire", "Light", "Paper", "Rainbow",
    "Smoke", "Snow", "Stars", "Dr. Wigglez", "Bubble Wand",
    "Charcoal", "Double Flat", "Space", "Wind"
]

CLASS_HINTS = {
    "Coarse Bristles": "speckled, stippled, coarse brush dots",
    "Duct Tape": "tape-like, adhesive strip, torn edges",
    "Fire": "flame, burning, fiery motion",
    "Light": "neon beam, glowing light streak",
    "Paper": "paper fiber texture, canvas-like, neutral base",
    "Rainbow": "spectrum colors, rainbow flow",
    "Smoke": "hazy, smoky particles, diffusion",
    "Snow": "falling snow particles, winter flakes",
    "Stars": "sparkling stars, night sky dots",
    "Dr. Wigglez": "wavy ribbon, wiggly motion, oscillation",
    "Bubble Wand": "bubbles, rings, tubular beads",
    "Charcoal": "charcoal grain, rough sketch, porous",
    "Double Flat": "double-sided color flip, planar stroke",
    "Space": "cosmic, oil-slick sheen, nebula vibes",
    "Wind": "streaming flow, trailing alpha, speed lines",
}

DEFAULT_BRUSH = "Paper"  # 백업 기본값

def choose_brush_by_llm(user_prompt: str, max_retries: int = 2) -> tuple[str, dict]:
    """
    LLM을 이용해 사용자 프롬프트에 가장 적합한 브러시를 15종류 중에서 선택합니다.
    - 최신 브러시 정보를 반영하여 판단 기준을 명확히 했습니다.
    - 출력: (선택된_브러시이름, 디버그정보)
    - 검증 실패 시 재시도 후, 최종 실패 시 DEFAULT_BRUSH를 반환합니다.
    """
    classes_str = ", ".join(BRUSH_CLASSES)

    # [수정됨] 최신 정보를 바탕으로 각 브러시의 핵심 특징을 상세하게 기술
    CLASS_HINTS = {
        "Coarse Bristles": "거친 입자, 짧은 터치, 점묘화 스타일 (Coarse, stippled, short touch)",
        "Duct Tape": "테이프 질감, 광택, 범프맵, 양면 느낌 (Tape-like, gloss, bump map)",
        "Fire": "활활 타오르는 불, 화염, 길게 늘어나는 동적 효과 (Burning flame, stretching animation)",
        "Light": "네온, 빛줄기, 광선, 길게 이어지는 라인 (Neon, light beam, continuous line)",
        "Paper": "기본적인 종이 질감, 페인트 칠, 캔버스 바탕 (Paper texture, paint, canvas)",
        "Rainbow": "무지개 색, 색상 순환 애니메이션, 여러 줄의 컬러 (Rainbow colors, cycling animation)",
        "Smoke": "퍼지는 연기 입자, 겹치면 밝아짐, 확산 효과 (Spreading smoke particles, additive blend)",
        "Snow": "내리는 눈, 낙하하는 입자, 겨울 분위기 (Falling snow, particles, winter)",
        "Stars": "반짝이는 별, 밤하늘, 고정된 입자의 반짝임 (Sparkling stars, night sky, twinkling particles)",
        "Dr. Wigglez": "꿈틀대는 리본, 흔들리는 웨이브, 흐르는 패턴 (Wiggling ribbon, wave motion)",
        "Bubble Wand": "비눗방울, 반투명 튜브, 연결된 구슬 형태 (Soap bubbles, translucent tube)",
        "Charcoal": "목탄 스케치, 구멍 뚫린 질감, 거친 드로잉 (Charcoal sketch, porous texture)",
        "Double Flat": "앞면과 뒷면 색상 반전, 시점에 따른 색상 대비 (Front/back color inversion, contrast)",
        "Space": "우주, 기름막(oil slick), 사이버펑크 광택, 색상 흐름 (Cosmic, oil slick, flowing colors)",
        "Wind": "바람의 흐름, 속도감, 뒤로 흩날리는 알파 (Air flow, speed lines, trailing alpha)",
    }

    # [수정됨] 더 구체적이고 까다로운 예시로 모델을 학습
    FEW_SHOTS = [
        ("연기처럼 퍼지는 회색 브러쉬", "Smoke"),
        ("반짝이는 별빛 점들이 흩뿌려진 느낌", "Stars"),
        ("물 위에 뜬 기름처럼 반짝이는 표면", "Space"),
        ("빨간색 네온사인처럼 그려줘", "Light"),
        ("활활 타오르는 불기둥", "Fire"),
        ("꿈틀거리면서 움직이는 리본", "Dr. Wigglez"),
        ("목탄으로 그린 것처럼 거친 스케치", "Charcoal"),
        ("바람에 흩날리는 효과", "Wind"),
        ("앞이랑 뒤 색깔이 다른 브러쉬", "Double Flat")
    ]

    system = (
        "You are a classifier that MUST choose EXACTLY ONE label from a CLOSED LIST based on its visual and technical characteristics.\n"
        "Task: Read the user's Korean prompt about a 3D brush effect and select the most appropriate brush NAME from the list below.\n"
        "Base your decision on the hints provided for each brush.\n"
        "Output RULES:\n"
        "- Output EXACTLY one of the labels, nothing else.\n"
        "- No quotes, no punctuation, no extra words.\n"
        "- If uncertain, pick the closest match based on the hints.\n\n"
        "LABEL LIST (choose one): " + classes_str + "\n\n"
        "HINTS (use these for your decision):\n" +
        "\n".join([f"- {k}: {v}" for k,v in CLASS_HINTS.items()])
    )

    messages = [{"role": "system", "content": system}]
    for s_in, s_out in FEW_SHOTS:
        messages.append({"role": "user", "content": s_in})
        messages.append({"role": "assistant", "content": s_out})
    messages.append({"role": "user", "content": user_prompt})

    # 재시도 및 검증 로직은 기존과 동일
    last_raw = None
    for attempt in range(1, max_retries + 1):
        try:
            resp = ollama.chat(
                model="llama3:8b",
                messages=messages,
                options={"temperature": 0.0, "top_p": 0.1}
            )
            raw = resp["message"]["content"].strip()
            last_raw = raw

            picked = None
            # 1. 정확히 일치하는 경우
            for cand in BRUSH_CLASSES:
                if cand.lower() == raw.lower():
                    picked = cand
                    break
            
            # 2. 포함하는 경우 (LLM이 부가 설명을 덧붙였을 때)
            if not picked:
                for cand in BRUSH_CLASSES:
                    if cand.lower() in raw.lower():
                        picked = cand
                        break

            if picked:
                return picked, {"reason": "llm", "attempt": attempt, "raw": raw}
            
            messages.append({"role": "system", "content": "FORMAT REMINDER: Output EXACTLY one label from the list, no extra text."})

        except Exception as e:
            last_raw = f"error: {e}"

    return DEFAULT_BRUSH, {"reason": "default", "raw": last_raw}




NAME_GENERATION_PROMPT = """
너는 사용자의 문장에서 **짧고 자연스러운 한국어 이름**을 추출하는 AI다. 
이름은 2~4단어로 된 **수식어+명사** 구조여야 한다.
반드시 생성 절차를 작성하지 말고 한줄로만 답변해야 한다.

### 알고리즘 단계:
1️⃣ **입력 문장 분석:** 문장에서 핵심 대상(사물, 재질, 상태)을 찾아낸다.  
2️⃣ **불필요한 단어 제거:** 아래와 같은 불필요한 단어를 모두 제거한다:  
   ["브러쉬", "만들어줘", "느낌", "같은", "같이", "스타일", "툴", "효과", "비슷한", "스러운", "플러스", "+"]  
3️⃣ **원문 표현 보존:** 의미를 바꾸지 말고, 원문의 단어를 그대로 유지한다.  
4️⃣ **구조 정리:** '수식어 + 명사' 형태로 자연스럽게 배열한다.  
5️⃣ **최종 출력:** 한국어로만, 2~4단어, 불필요한 기호 없이 한 줄로 출력한다.

### 출력 예시:
- 입력: "깨끗한 천 브러쉬" → 출력: 깨끗한 천
- 입력: "용암 같이 흐르는 플러스" → 출력: 흐르는 용암
- 입력: "곰팡이 핀 벽 브러쉬" → 출력: 곰팡이 핀 벽
- 입력: "금속이 끓는듯한 브러쉬 만들어 줘" → 출력: 끓는 금속
- 입력: "빛나는 수정 같은 브러쉬 스타일" → 출력: 빛나는 수정
- 입력: "빨간색 립스틱" → 출력: 빨간색 립스틱
"""



class Prompt(BaseModel):
    prompt: str




template = """
You are an AI that rewrites user texture requests into a **single-line English prompt** for AI texture generation models.

Your output must be:
- one single line
- without any prefix (no "Output:", no "Prompt:", etc.)
- not enclosed in quotes
- Never answer in English only
- You should never answer in Korean
- Follow this format:
seamless [color/style] texture, [texture detail], [surface quality], stylized, high resolution, top-down, suitable for Unity material texture

NEVER include the original input or explanation.
JUST give the result in that exact format.

User_prompt: {user_prompt}
"""



@app.post("/generate")
async def generate(prompt: Prompt):  # prompt: { "prompt": "반짝이는 우주 브러쉬 만들어줘" }
    try:
        print("📥 입력 받은 프롬프트:", prompt.prompt)

        processed_prompt = prompt.prompt.replace("플래시", "브러쉬")
        if processed_prompt != prompt.prompt:
            print(f"🔄 입력어 자동 변환: '{prompt.prompt}' -> '{processed_prompt}'")
        
        selected_brush, dbg = choose_brush_by_llm(processed_prompt)
        print(f"🧭 선택된 브러시: {selected_brush} | 디버그: {dbg}")


        # 1. Ollama를 사용해 '브러시 이름' 생성
        print("🏷️  브러시 이름 생성 요청 중...")
        name_response = ollama.chat(
            model=model, # 기존에 사용하던 모델 변수
            messages=[
                {"role": "system", "content": NAME_GENERATION_PROMPT},
                {"role": "user", "content": processed_prompt}
            ]
        )
        brush_name = name_response['message']['content'].strip()
        print(f"✅ 생성된 브러시 이름: {brush_name}")

        # Ollama로 프롬프트 재작성 요청
        ollama_response = ollama.chat(
            model=model,
            messages=[
                {"role": "system", 
                "content":
                    "You are an AI that rewrites user texture requests into highly detailed prompts for AI texture generation models.\n\n"
                    "Your goal is to produce a seamless texture suitable for Unity 3D brush materials.\n"
                    "The style should include:\n"
                    "- soft, flowing brush-like surface\n"
                    "- raised or layered brush texture\n"
                    "- clean, reflective or slightly rough surfaces\n"
                    "- vivid or stylized colors\n"
                    "- suitable for digital painting tools\n\n"
                    "Avoid including any text or letters in the image. Do not generate images of brush tools themselves.\n"
                    "Focus on the texture result — not the physical brush.\n"
                    "You MUST output in **English only**, even if the user input is in Korean. NEVER use Korean."
                },
                {"role": "user", "content": template.format(user_prompt=prompt.prompt)}
            ]

        )
        refined_prompt = ollama_response['message']['content'].strip()
        
        print("🧠 생성된 refined_prompt:", refined_prompt)

        # JSON 워크플로우 불러오기
        try:
            with open("tiled_workflow.json", "r", encoding="utf-8") as f:
                workflow_data = json.load(f)
        except FileNotFoundError:
            print("❌ 워크플로우 파일(tiled_workflow.json)을 찾을 수 없습니다.")
            raise HTTPException(status_code=500, detail="Workflow file not found.")

        print("📂 워크플로우 로딩 완료. 노드 키 목록:", list(workflow_data.keys()))

        # 프롬프트 삽입
        if "3" not in workflow_data:
            print("❌ 노드 '3'이 워크플로우에 존재하지 않습니다.")
            raise HTTPException(status_code=500, detail="Node '3' not found in workflow.")
        if "inputs" not in workflow_data["3"]:
            print("❌ 노드 '3'에 'inputs' 항목이 없습니다.")
            raise HTTPException(status_code=500, detail="Node '3' is malformed.")
        
        workflow_data["3"]["inputs"]["text"] = refined_prompt
        print("🖊️ 워크플로우에 프롬프트 삽입 완료")

        # ComfyUI 실행
        print("🚀 ComfyUI 실행 요청 중...")
        prompt_id = queue_prompt(workflow_data, COMFYUI_IP)
        print("🆔 받은 prompt_id:", prompt_id)

        result = await check_progress(prompt_id, COMFYUI_IP)
        print("📦 ComfyUI 처리 완료, 결과 수신")

        # 결과 이미지 추출
        print("🔍 결과에서 이미지 URL 추출 시도")
        for node_id, node_output in result["outputs"].items():
            if "images" in node_output:
                filename = node_output['images'][0]['filename']
                image_url = f"http://{COMFYUI_IP}/view?filename={filename}&type=temp"
                print("🖼️ 최종 이미지 URL:", image_url)

                try:
                    return {"status": "completed", "image": image_url, "name": brush_name, "type": selected_brush}  # key 이름도 image_url 그대로 유지
                except Exception as e:
                    print("❌ 이미지 다운로드 또는 인코딩 중 오류:", str(e))
                    raise HTTPException(status_code=500, detail="Image fetch or encoding failed.")



        print("⚠️ 결과에는 이미지가 포함되지 않았습니다.")
        return {"status": "completed", "image": None, "name": None, "type": None}

    except Exception as e:
        print("❌ 예외 발생:", repr(e))
        raise HTTPException(status_code=500, detail=str(e))



def queue_prompt(prompt_workflow, ip):
    p = {"prompt": prompt_workflow}
    data = json.dumps(p).encode('utf-8')
    req = request.Request(f"http://{ip}/prompt", data=data)
    try:
        res = request.urlopen(req)
        if res.code != 200:
            raise Exception(f"Error: {res.code} {res.reason}")
        return json.loads(res.read().decode('utf-8'))['prompt_id']
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


async def check_progress(prompt_id: str, ip: str):
    while True:
        try:
            req = request.Request(f"http://{ip}/history/{prompt_id}")
            res = request.urlopen(req)
            if res.code == 200:
                history = json.loads(res.read().decode('utf-8'))
                if prompt_id in history:
                    return history[prompt_id]
        except Exception as e:
            print(f"Error checking progress: {str(e)}")
        await asyncio.sleep(1)


#------------------ AI 브러시 -----------------------





#------------------ AI 색상 팔렛트 -----------------------

class TagRequest(BaseModel):
    tag: str

# 전역 변수로 로드 (최초 1회)
COLOR_DB_PATH = Path("color.json")
with COLOR_DB_PATH.open(encoding="utf-8") as f:
    PRESET_TAG_COLORS = json.load(f)


@app.post("/generate-colors-by-tag")
async def generate_colors(data: TagRequest):
    tag = data.tag.strip()

    # 유사도 측정 우선
    best_match = None
    best_score = 0
    for preset_tag in PRESET_TAG_COLORS:
        score = fuzz.partial_ratio(tag, preset_tag)
        if score > best_score:
            best_score = score
            best_match = preset_tag

    print(f"🧠 유사도 최고값: {best_score} ({best_match})")

    # 유사도가 70 이상이면 미리 정의된 색상 사용
    if best_score >= 70:
        return {"hex_list": PRESET_TAG_COLORS[best_match]}

    # 그 외에는 LLM 호출
    prompt = f"""
You are a professional color theorist and visual designer.

Your task is to select 10 unique HEX color codes that best represent the visual identity and emotional tone of the style: '{tag}'.

The colors should:
- Match the artistic and cultural context associated with '{tag}'
- Reflect visual harmony and appropriate emotional impact
- Include diverse tones (avoid overly similar shades)

Instructions:
- Only output 10 HEX color codes, separated by commas
- Do not include any explanations or extra text

Example output:
#C41E3A, #F5C842, #FFB6CC, #FFD7C9, #FFA07A, #E4FAF5, #F9F98F, #FFD36A, #FFCEB0, #AEDB9C
"""

    def extract_hex_codes(text: str) -> list[str]:
        return list(dict.fromkeys(re.findall(r"#(?:[0-9a-fA-F]{6})", text)))

    attempt = 1
    while True:
        response = ollama.chat(
            model=model,
            messages=[{"role": "user", "content": prompt}]
        )

        text = response["message"]["content"]
        text = re.sub(r"```.*?```", "", text, flags=re.DOTALL)
        hex_list = extract_hex_codes(text)

        print(f"🔁 시도 {attempt}: {len(hex_list)}개 추출됨")
        print("🧾 응답:", text)
        print("🎨 HEX 추출:", hex_list)
        attempt += 1

        if len(hex_list) >= 10:
            return {"hex_list": hex_list[:10]}




#------------------ AI 색상 팔렛트 -----------------------


#------------------ AI 색상 주제 -----------------------

class SchemeRequest(BaseModel):
    hex: str
    mode: str
    count: int = 10


@app.post("/color-scheme")
async def get_color_scheme(data: SchemeRequest):
    url = "https://www.thecolorapi.com/scheme"
    params = {
        "hex": data.hex,
        "mode": data.mode,
        "count": data.count
    }

    async with httpx.AsyncClient() as client:
        response = await client.get(url, params=params)

    if response.status_code != 200:
        return {"error": f"Failed to fetch scheme: {response.text}"}

    full_data = response.json()
    hex_list = [color["hex"]["value"] for color in full_data.get("colors", [])]

    return {"hex_list": hex_list}



#------------------ AI 색상 주제 -----------------------


def filter_to_hangul_only(text: str) -> str:
    return re.sub(r"[^가-힣\s.,!?]", "", text)


#------------------ AI UI 챗봇 팔렛트 -----------------------


def get_tts_url_from_cloud(text: str) -> str:
    cloud_run_tts_url = os.getenv("CLOUD_RUN_TTS_URL")
    if not cloud_run_tts_url:
        print("CLOUD_RUN_TTS_URL is not set.")
        return None
    try:
        response = requests.post(cloud_run_tts_url, json={"text": text}, timeout=10)
        if response.ok:
            return response.json().get("audio")  # 이 URL에는 UUID가 들어감
        else:
            print("⚠️ 상태코드:", response.status_code)
            return None
    except Exception as e:
        print("❌ TTS 요청 실패:", e)
        return None




# CSV 로드 및 전처리
# 1️⃣ 쉼표 기준으로 직접 열 파싱 (기본 sep=',')
df_split = pd.read_csv("docs/UIDocs.csv", encoding="utf-8-sig")

# 2️⃣ 발음 오류 매핑 CSV도 동일하게 로딩
correction_df = pd.read_csv("docs/PronunciationCorrection.csv", encoding="utf-8-sig")




class Question(BaseModel):
    query: str


def generate_friendly_explanation(user_query: str, row: pd.Series, model: str = "llama3:8b") -> Optional[str]:
    # (이 부분은 기존 코드와 동일합니다)
    explanation_keywords = ["무엇", "뭐야", "무슨 뜻", "설명", "정의", "이게 뭐야", "기능", "역할"]
    action_keywords = ["어디", "위치", "누르면", "클릭", "눌러", "열어", "방법", "사용법", "조작", "하는 법"]
    user_query_lower = user_query.lower()
    if any(kw in user_query_lower for kw in action_keywords):
        is_explanation = False
    else:
        is_explanation = any(kw in user_query_lower for kw in explanation_keywords)
    explanation_text = str(row.get("설명/메뉴", "")).strip()
    action_text = str(row.get("동작 형태", "")).strip()
    
    # 프롬프트 부분을 수정합니다.
    if is_explanation and explanation_text:
        target_text = explanation_text
        prompt = f"""[지시]
당신은 다음 [정보]를 VR 사용자에게 안내할 단 한 개의 친절하고 자연스러운 문장으로 바꿔야 합니다.

[정보]
{target_text}

[규칙]
- 반드시 **한국어**로 답변하세요.
- 반드시 한 문장으로만 답변하세요.
- "답변:", "출력:" 같은 접두사를 사용하지 마세요.

[결과]
"""
    else:
        target_text = action_text
        prompt = f"""[지시]
당신은 다음 [정보]를 보고, VR 사용자가 어떻게 행동해야 하는지 알려주는 단 한 개의 친절하고 자연스러운 문장을 만들어야 합니다.

[정보]
{target_text}

[규칙]
- 반드시 **한국어**로 답변하세요.
- 반드시 한 문장으로만 답변하세요.
- "답변:", "출력:" 같은 접두사를 사용하지 마세요.

[결과]
"""
    
    # (이하 후처리 로직은 기존 코드와 동일합니다)
    print("🔍 질문:", user_query)
    print("📌 분류:", "설명" if is_explanation else "동작")
    print(f"📄 타겟 텍스트: [{target_text}]")
    try:
        response = ollama.chat(
            model=model,
            messages=[{"role": "user", "content": prompt}]
        )
        raw_answer = response["message"]["content"].strip()
        first_line = raw_answer.split('\n')[0].strip()
        prefixes_to_remove = ["[결과]", "결과:", "답변:", "출력:", "설명:", "AI가 사용자에게 말하기를:"]
        clean_answer = first_line
        for prefix in prefixes_to_remove:
            if clean_answer.startswith(prefix):
                clean_answer = clean_answer[len(prefix):].strip()
        return clean_answer
    except Exception as e:
        print(f"❌ 설명 생성 오류: {e}")
        return target_text
    


def get_friendly_response(row, user_query: str, already_generated: set):
    explanation = generate_friendly_explanation(user_query, row)
    fallback = row.get("설명/메뉴") or row.get("동작 형태") or ""
    answer = explanation or fallback

    if answer in already_generated:
        audio = None
    else:
        audio = get_tts_url_from_cloud(answer)
        already_generated.add(answer)

    return {
        "answer": answer,
        "source": row["UI 요소명"],
        "audio": audio
    }


@app.post("/rag")
async def rag_search(q: Question):
    user_query = q.query.strip()

    already_generated = set()  # 한 요청 내 중복 방지용

    # 정확 일치
    exact_match = df_split[df_split["UI 요소명"] == user_query]
    if not exact_match.empty:
        row = exact_match.iloc[0]
        return get_friendly_response(row, user_query, already_generated)

    # 유사도 기반 포함 매칭
    best_score = 0
    best_row = None
    for _, row in df_split.iterrows():
        ui_name = row["UI 요소명"]
        score = fuzz.partial_ratio(user_query, ui_name)
        if score > best_score:
            best_score = score
            best_row = row

    if best_score >= 70:
        print(f"🧠 유사도 기반 매칭: {best_row['UI 요소명']} (score={best_score})")
        return get_friendly_response(best_row, user_query, already_generated)

    # 발음 교정 후 재탐색
    corrected_query = user_query
    for _, row in correction_df.iterrows():
        if row["발음오류"] in corrected_query:
            corrected_query = corrected_query.replace(row["발음오류"], row["정정어"])

    if corrected_query != user_query:
        print(f"🛠 교정 적용: '{user_query}' → '{corrected_query}'")
        return await rag_search(Question(query=corrected_query))

    # 일반 챗봇 호출
    print(f"❓ 모든 매칭 실패 → 일반 챗봇(ollama) 호출")
    try:
        prompt = f"""[지시]
당신은 **VR 환경에서 3D 페인팅 그림을 그리는 아티스트**의 질문에 대해 친절하고 자연스러운 **한 문장**의 한국어 응답을 만들어야 합니다.

[사용자 질문]
{user_query}

[규칙]
- 반드시 **한국어**로 답변하세요.
- 반드시 **한 문장으로만** 답변하세요.
- 반드시 **VR 사용자의 맥락에 맞는** 설명을 제공하세요.
- 사용자는 VR 컨트롤러로 상호작용하는점 명심해주세요.
- **영어, 일본어, 한자, 숫자, 기호 등 한글 이외의 문자는 사용하지 마세요.**
- "답변:", "출력:" 같은 접두사를 사용하지 마세요.
- 너무 많은 설명을 피하고, 사용자의 의도를 자연스럽게 요약하세요.
- 창의적이거나 시적인 표현은 피하고, 실제 행동이나 방법을 설명하세요.
- 그림이나 예술과 관련된 질문도 실용적인 설명 위주로 답변하세요.

[결과]
"""

        response = ollama.chat(
            model="llama3:8b",
            messages=[{"role": "user", "content": prompt}]
        )

        raw_answer = response["message"]["content"].strip()
        first_line = raw_answer.split('\n')[0].strip()
        prefixes_to_remove = ["[결과]", "결과:", "답변:", "출력:", "설명:", "AI가 사용자에게 말하기를:"]
        clean_answer = first_line
        for prefix in prefixes_to_remove:
            if clean_answer.startswith(prefix):
                clean_answer = clean_answer[len(prefix):].strip()

        filtered = filter_to_hangul_only(clean_answer)
        audio_url = get_tts_url_from_cloud(filtered)  # 항상 새로 생성

        return {
            "answer": filtered,
            "source": "chatbot",
            "audio": audio_url
        }

    except Exception as e:
        print(f"❌ ollama 챗봇 처리 중 오류: {e}")
        return {
            "answer": "죄송합니다. 챗봇 처리 중 오류가 발생했습니다.",
            "source": "chatbot-error",
            "audio": None
        }





#------------------ AI UI 챗봇 팔렛트 -----------------------

# fast api 서울
# CUDA_VISIBLE_DEVICES=0 uvicorn convert_prompt:app --host 0.0.0.0 --port 58021

# fast api 대구
# uvicorn convert_prompt:app --reload --port 8080

# ollama 서울
# CUDA_VISIBLE_DEVICES=0 ollama serve

# ollama 대구
# ollama serve

# comfyui 서울
# CUDA_VISIBLE_DEVICES=0 python main.py --port 58022 --listen 0.0.0.0

# comfyui 대구
# python main.py

# conda activate yoon
