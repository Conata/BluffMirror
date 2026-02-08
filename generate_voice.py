#!/usr/bin/env python3
"""
ElevenLabs API を使用してトランプ風のボイスラインを生成
日本語と英語の両方に対応
"""

import os
import requests
from pathlib import Path
from dotenv import load_dotenv

# .env ファイルから環境変数を読み込む
load_dotenv()

ELEVEN_API_KEY = os.getenv("ELEVEN_API_KEY")
if not ELEVEN_API_KEY:
    raise ValueError("ELEVEN_API_KEY が .env ファイルに設定されていません")

# ElevenLabs API エンドポイント
ELEVEN_API_URL = "https://api.elevenlabs.io/v1/text-to-speech"

# 出力先ディレクトリ
OUTPUT_DIR = Path("Baba/Assets/Audio/Voice")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# デフォルトボイスID（トランプ風の声）
# ※ ElevenLabsで適切なボイスを選択してください
# 以下はプレースホルダーです
DEFAULT_VOICE_ID = "21m00Tcm4TlvDq8ikWAM"  # Rachel (置き換えてください)

def generate_voice(text: str, filename: str, voice_id: str = DEFAULT_VOICE_ID, language: str = "en"):
    """
    ElevenLabs APIを使用して音声を生成

    Args:
        text: 音声にしたいテキスト
        filename: 保存するファイル名（拡張子なし）
        voice_id: 使用するボイスのID
        language: 言語 ("en" または "ja")
    """
    url = f"{ELEVEN_API_URL}/{voice_id}"

    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": ELEVEN_API_KEY
    }

    # 言語に応じてモデルを選択
    model_id = "eleven_multilingual_v2" if language == "ja" else "eleven_monolingual_v1"

    data = {
        "text": text,
        "model_id": model_id,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
            "style": 0.5,
            "use_speaker_boost": True
        }
    }

    print(f"生成中: {filename} ({language}) - \"{text}\"")

    response = requests.post(url, json=data, headers=headers)

    if response.status_code == 200:
        output_path = OUTPUT_DIR / f"{filename}.mp3"
        with open(output_path, "wb") as f:
            f.write(response.content)
        print(f"✓ 保存完了: {output_path}")
    else:
        print(f"✗ エラー: {response.status_code} - {response.text}")

def main():
    """メイン関数 - すべての音声を生成"""

    # ============================================
    # 英語ボイスライン
    # ============================================
    english_lines = {
        # ゲーム開始
        "game_start_1": "Let's play. I'm the best at this game, believe me.",
        "game_start_2": "You ready? This is going to be huge!",

        # カードドロー
        "card_draw_1": "Oh, this is a good one!",
        "card_draw_2": "Tremendous card!",
        "card_draw_3": "Nobody draws better cards than me.",

        # ペア成立
        "pair_match_1": "Perfect match! Just like I planned.",
        "pair_match_2": "See? I told you. The best.",

        # 勝利
        "victory_1": "I won! Of course I won. I always win!",
        "victory_2": "Too easy. Nobody beats me at cards.",

        # 敗北
        "defeat_1": "This game is rigged! Totally unfair!",
        "defeat_2": "I demand a recount!",

        # 心理圧
        "pressure_1": "You're sweating. I can tell.",
        "pressure_2": "Feeling the pressure? You should be.",
        "pressure_3": "This is what winning looks like.",
    }

    # ============================================
    # 日本語ボイスライン
    # ============================================
    japanese_lines = {
        # ゲーム開始
        "game_start_1_ja": "始めよう。私はこのゲームで最高だ、信じてくれ。",
        "game_start_2_ja": "準備はいいか？これは凄いことになるぞ！",

        # カードドロー
        "card_draw_1_ja": "おお、これは良いカードだ！",
        "card_draw_2_ja": "素晴らしいカードだ！",
        "card_draw_3_ja": "私ほど良いカードを引く者はいない。",

        # ペア成立
        "pair_match_1_ja": "完璧なマッチだ！計画通りだ。",
        "pair_match_2_ja": "ほら、言っただろう。最高だ。",

        # 勝利
        "victory_1_ja": "勝った！当然だ。私はいつも勝つ！",
        "victory_2_ja": "簡単すぎる。カードで私に勝てる者はいない。",

        # 敗北
        "defeat_1_ja": "このゲームは不正だ！完全に不公平だ！",
        "defeat_2_ja": "再集計を要求する！",

        # 心理圧
        "pressure_1_ja": "汗をかいているな。見てわかるぞ。",
        "pressure_2_ja": "プレッシャーを感じているか？感じるべきだ。",
        "pressure_3_ja": "これが勝利というものだ。",
    }

    print("=" * 60)
    print("ElevenLabs 音声生成スクリプト")
    print("=" * 60)
    print()

    # 英語音声を生成
    print("[ 英語ボイスライン生成中 ]")
    print("-" * 60)
    for filename, text in english_lines.items():
        generate_voice(text, filename, language="en")

    print()

    # 日本語音声を生成
    print("[ 日本語ボイスライン生成中 ]")
    print("-" * 60)
    for filename, text in japanese_lines.items():
        generate_voice(text, filename, language="ja")

    print()
    print("=" * 60)
    print("✓ すべての音声生成が完了しました")
    print(f"✓ 保存先: {OUTPUT_DIR.absolute()}")
    print("=" * 60)

if __name__ == "__main__":
    main()
