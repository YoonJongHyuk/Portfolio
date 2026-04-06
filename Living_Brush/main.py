from fastapi import FastAPI
from pydantic import BaseModel
import pandas as pd
from rag_chain import create_rag_chain

app = FastAPI()
qa_chain = create_rag_chain()

# CSV 로드 및 전처리
df = pd.read_csv("docs/UIDocs.csv", encoding="utf-8-sig")
df_split = df.iloc[:, 0].str.split('\t', expand=True)
df_split.columns = ["UI 요소명", "Figma 프레임명", "위치", "지원 및 연결 패널", "동작 형태"]

class Question(BaseModel):
    query: str

@app.post("/rag")
async def rag_search(q: Question):
    user_query = q.query.strip()

    # 👉 CSV에 있는 "UI 요소명"과 질문 비교
    for _, row in df_split.iterrows():
        if row["UI 요소명"] in user_query:
            return {
                "answer": row["동작 형태"],
                "source": row["UI 요소명"]
            }

    # 👉 일치하는 요소가 없다면 RAG로 응답
    result = qa_chain.invoke({"query": user_query})
    return {
        "answer": result["result"],
        "sources": [doc.metadata for doc in result["source_documents"]]
    }
