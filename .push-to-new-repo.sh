#!/bin/bash

echo "🎮 BluffMirror - 新しいリポジトリへのプッシュ"
echo "================================================"
echo ""

# 既存のGit履歴をバックアップ
if [ -d .git ]; then
    echo "📦 Backing up old .git directory..."
    mv .git .git_backup_$(date +%Y%m%d_%H%M%S)
    echo "✅ Backup created"
fi

# 新しいリポジトリを初期化
echo ""
echo "🔨 Initializing new repository..."
git init
git branch -M main

# 全ファイルをステージング
echo ""
echo "📝 Staging all files..."
git add .

# 初回コミット
echo ""
echo "💾 Creating initial commit..."
git commit -m "🎮 Initial commit: Bluff Mirror - FPS Psychological Old Maid Game

- Unity 6 LTS (6000.0.x) + URP rendering pipeline
- AI-powered mentalist system with Claude Vision API
- Real-time facial expression analysis (Unity Sentis + FERPlus)
- Chain-of-Thought card selection system
- Personality diagnosis with fortune-telling integration
- Bluff action system with psychological warfare
- Bilingual support (Japanese/English)
- Camera cinematics with Cinemachine 3.x
- Live2D character integration

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# リモートを追加
echo ""
echo "🌐 Adding remote repository..."
git remote add origin https://github.com/Conata/BluffMirror.git

# 最終確認
echo ""
echo "✅ Repository prepared!"
echo ""
echo "📋 Next steps:"
echo "1. Verify all API keys have been rotated"
echo "2. Update .env file with new API keys"
echo "3. Run: git push -u origin main"
echo ""
echo "⚠️  FINAL CHECK: Run 'git log -p | grep \"sk-\" | head -20' to ensure no API keys in history"
