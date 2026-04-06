import os
from langchain.schema import Document
import pandas as pd
from langchain_community.vectorstores import FAISS
from langchain_community.cross_encoders import HuggingFaceCrossEncoder
from langchain.retrievers.document_compressors import CrossEncoderReranker
from langchain.retrievers import ContextualCompressionRetriever
from langchain.chains import RetrievalQA
from langchain_ollama import OllamaLLM as Ollama
from langchain_huggingface import HuggingFaceEmbeddings


def create_rag_chain():
    vector_path = "vectorstore/ui_vectors"
    faiss_file = os.path.join(vector_path, "index.faiss")

    # 임베딩 모델 로드
    embedding_model = HuggingFaceEmbeddings(model_name="all-MiniLM-L6-v2")

    # 벡터 DB 로드 또는 생성
    try:
        db = FAISS.load_local(vector_path, embedding_model, allow_dangerous_deserialization=True)
        print("✅ 기존 벡터 DB 로드 완료")
    except Exception as e:
        print("⚠️ 벡터 DB 로드 실패, 새로 생성합니다:", e)

        # 폴더가 없으면 생성
        os.makedirs(vector_path, exist_ok=True)

        # CSV 읽고 문서화
        df = pd.read_csv("docs/UIDocs.csv", encoding="utf-8-sig")
        df.columns = [col.replace('\ufeff', '') for col in df.columns]

        docs = [
            Document(
                page_content=str(row['UI 요소명']).strip(),
                metadata={"source": "UIDocs.csv", "ui_name": row['UI 요소명']}
            )
            for _, row in df.iterrows()
            if pd.notna(row['UI 요소명']) and str(row['UI 요소명']).strip() != ""
        ]

        db = FAISS.from_documents(docs, embedding_model)
        db.save_local(vector_path)
        print("✅ 벡터 DB 새로 생성 및 저장 완료")

    # 리랭커 및 압축 리트리버 설정
    reranker_model = HuggingFaceCrossEncoder(model_name="cross-encoder/ms-marco-MiniLM-L-6-v2")
    compressor = CrossEncoderReranker(model=reranker_model)
    retriever = ContextualCompressionRetriever(
        base_retriever=db.as_retriever(),
        base_compressor=compressor
    )

    # QA 체인 구성
    llm = Ollama(model="llama3:8b")
    chain = RetrievalQA.from_chain_type(
        llm=llm,
        retriever=retriever,
        return_source_documents=True
    )

    return chain