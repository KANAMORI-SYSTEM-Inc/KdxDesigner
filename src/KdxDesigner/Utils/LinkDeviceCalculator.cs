using System;
using System.Linq;

namespace KdxDesigner.Utils
{
    /// <summary>
    /// PLCデバイスアドレスの解析とフォーマットを行うヘルパークラス。
    /// </summary>
    public static class LinkDeviceCalculator
    {
        /// <summary>
        /// "X800", "Y1AFF" のような16進数アドレスを、プレフィックスと数値に分解します。
        /// </summary>
        /// <returns>成功した場合はtrue、それ以外はfalse。</returns>
        public static bool TryParseHexAddress(string? address, out string prefix, out long value)
        {
            value = 0;
            prefix = string.Empty;
            if (string.IsNullOrEmpty(address)) return false;

            // 文字列の先頭から、数字(0-9)または16進文字(A-F)でない部分をプレフィックスとして抽出
            prefix = new string(address.TakeWhile(c => !Uri.IsHexDigit(c)).ToArray());

            if (string.IsNullOrEmpty(prefix)) return false;

            string numberPart = address.Substring(prefix.Length);
            return long.TryParse(numberPart, System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        /// <summary>
        /// "W0010.0", "B123A.F" のようなリンク先アドレスを、プレフィックスと合計ビットオフセット値に分解します。
        /// </summary>
        /// <returns>成功した場合はtrue、それ以外はfalse。</returns>
        public static bool TryParseLinkAddress(string? address, out string prefix, out long totalBitOffset)
        {
            totalBitOffset = 0;
            prefix = string.Empty;
            if (string.IsNullOrEmpty(address) || !address.Contains(".")) return false;

            var dotIndex = address.LastIndexOf('.');
            string wordPartStr = address.Substring(0, dotIndex);
            string bitPartStr = address.Substring(dotIndex + 1);

            prefix = new string(wordPartStr.TakeWhile(c => !Uri.IsHexDigit(c)).ToArray());
            if (string.IsNullOrEmpty(prefix)) return false;

            string wordNumberPart = wordPartStr.Substring(prefix.Length);

            if (long.TryParse(wordNumberPart, System.Globalization.NumberStyles.HexNumber, null, out long word) &&
                long.TryParse(bitPartStr, System.Globalization.NumberStyles.HexNumber, null, out long bit))
            {
                // 合計ビットオフセット = (ワード値 * 16) + ビット値
                totalBitOffset = (word * 16) + bit;
                return true;
            }
            return false;
        }

        /// <summary>
        /// プレフィックスと合計ビットオフセット値から、"W0010.F" のようなリンクアドレス文字列を生成します。
        /// </summary>
        public static string FormatLinkAddress(string prefix, long totalBitOffset)
        {
            long word = totalBitOffset / 16;
            long bit = totalBitOffset % 16;

            // X4は4桁の16進数、Xは1桁の16進数を意味する
            return $"{prefix}{word:X4}.{bit:X}";
        }
    }
}