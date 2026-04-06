from fastapi import FastAPI
from pydantic import BaseModel
import httpx

app = FastAPI()

class SchemeRequest(BaseModel):
    hex: str
    mode: str
    count: int = 5

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
