#!/bin/sh
case "$GIT_COMMIT" in
 e343076*)
  echo "テスト(app): BitmapDisplayRenderer の基本動作確認用のコンソール スモークテストを追加（暫定）" ;;
 851816b*)
  echo "テスト(app): BitmapDisplayRenderer の基本動作確認用のコンソール スモークテストを追加" ;;
 e2e7d79*)
  echo "雑務(test): BitmapDisplayRenderer の xUnit テスト追加および DisplayManager コンストラクタのレンダラ必須化" ;;
 2c241a6*)
  echo "リファクタ(renderer): DisplayManager のコンストラクタでレンダラ必須化、DisplayRenderRequest を幅/高さのプリミティブとサービスに変更、IStringService を導入" ;;
 *)
  cat ;;
esac
