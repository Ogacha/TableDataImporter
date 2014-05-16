using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableDataImporter
{

    public enum CharCode
    {
        ASCII,
        BINARY,
        EUC,
        JIS,
        SJIS,
        UTF16BE,
        UTF16LE,
        UTF8N
    }

    public class 文字コード判定
    {
        // こっちは DOBON Jcode.pm の移植 http://dobon.net/vb/dotnet/string/detectcode.html
        /// <summary>
        /// 文字コードを判別する
        /// </summary>
        /// <remarks>
        /// Jcode.pmのgetcodeメソッドを移植したものです。
        /// Jcode.pm(http://openlab.ring.gr.jp/Jcode/index-j.html)
        /// Jcode.pmのCopyright: Copyright 1999-2005 Dan Kogai
        /// </remarks>
        /// <param name="bytes">文字コードを調べるデータ</param>
        /// <returns>適当と思われるEncodingオブジェクト。
        /// 判断できなかった時はnull。</returns>
        public static Encoding GetCode(byte[] bytes)
        {
            const byte bEscape = 0x1B;
            const byte bAt = 0x40;
            const byte bDollar = 0x24;
            const byte bAnd = 0x26;
            const byte bOpen = 0x28;    //'('
            const byte bB = 0x42;
            const byte bD = 0x44;
            const byte bJ = 0x4A;
            const byte bI = 0x49;

            int len = bytes.Length;
            byte b1, b2, b3, b4;

            //Encode::is_utf8 は無視

            bool isBinary = false;
            for (int i = 0; i < len; i++)
            {
                b1 = bytes[i];
                if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF)
                {
                    //'binary'
                    isBinary = true;
                    if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F)
                    {
                        //smells like raw unicode
                        return System.Text.Encoding.Unicode;
                    }
                }
            }
            if (isBinary)
            {
                return null;
            }

            //not Japanese
            bool notJapanese = true;
            for (int i = 0; i < len; i++)
            {
                b1 = bytes[i];
                if (b1 == bEscape || 0x80 <= b1)
                {
                    notJapanese = false;
                    break;
                }
            }
            if (notJapanese)
            {
                return System.Text.Encoding.ASCII;
            }

            for (int i = 0; i < len - 2; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                b3 = bytes[i + 2];

                if (b1 == bEscape)
                {
                    if (b2 == bDollar && b3 == bAt)
                    {
                        //JIS_0208 1978
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bDollar && b3 == bB)
                    {
                        //JIS_0208 1983
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && (b3 == bB || b3 == bJ))
                    {
                        //JIS_ASC
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && b3 == bI)
                    {
                        //JIS_KANA
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    if (i < len - 3)
                    {
                        b4 = bytes[i + 3];
                        if (b2 == bDollar && b3 == bOpen && b4 == bD)
                        {
                            //JIS_0212
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                        if (i < len - 5 &&
                            b2 == bAnd && b3 == bAt && b4 == bEscape &&
                            bytes[i + 4] == bDollar && bytes[i + 5] == bB)
                        {
                            //JIS_0208 1990
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                    }
                }
            }

            //should be euc|sjis|utf8
            //use of (?:) by Hiroki Ohzaki <ohzaki@iod.ricoh.co.jp>
            int sjis = 0;
            int euc = 0;
            int utf8 = 0;
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
                    ((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC)))
                {
                    //SJIS_C
                    sjis += 2;
                    i++;
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
                    (b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF)))
                {
                    //EUC_C
                    //EUC_KANA
                    euc += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = bytes[i + 2];
                    if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
                        (0xA1 <= b3 && b3 <= 0xFE))
                    {
                        //EUC_0212
                        euc += 3;
                        i += 2;
                    }
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF))
                {
                    //UTF8
                    utf8 += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = bytes[i + 2];
                    if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
                        (0x80 <= b3 && b3 <= 0xBF))
                    {
                        //UTF8
                        utf8 += 3;
                        i += 2;
                    }
                }
            }
            //M. Takahashi's suggestion
            //utf8 += utf8 / 2;

            System.Diagnostics.Debug.WriteLine(
                string.Format("sjis = {0}, euc = {1}, utf8 = {2}", sjis, euc, utf8));
            if (euc > sjis && euc > utf8)
            {
                //EUC
                return Encoding.GetEncoding(51932);
            }
            else if (sjis > euc && sjis > utf8)
            {
                //SJIS
                return Encoding.GetEncoding(932);
            }
            else if (utf8 > euc && utf8 > sjis)
            {
                //UTF8
                return Encoding.UTF8;
            }

            return null;
        }

        // こっちは
        // http://d.hatena.ne.jp/hnx8/20120225/1330157903
        // まだちゃんと見てないし、使ってない

        /// <summary>
        /// 読み込んでいるbyte配列内容のエンコーディングを自前で判定する
        /// </summary>
        /// <param name="data">ファイルから読み込んだバイトデータ</param>
        /// <param name="datasize">バイトデータのサイズ</param>
        /// <returns>エンコーディングの種類</returns>
        public CharCode detectCharCode(byte[] data, int datasize)
        {
            //バイトデータ（読み取り結果）
            byte b1 = (datasize > 0) ? data[0] : (byte)0;
            byte b2 = (datasize > 1) ? data[1] : (byte)0;
            byte b3 = (datasize > 2) ? data[2] : (byte)0;
            byte b4 = (datasize > 3) ? data[3] : (byte)0;

            //UTF16Nの判定(ただし半角英数文字の場合のみ検出可能)
            if (b1 == 0x00 && (datasize % 2 == 0))
            {
                for (int i = 0; i < datasize; i = i + 2)
                {
                    if (data[i] != 0x00 || data[i + 1] < 0x06 || data[i + 1] >= 0x7f)
                    {   //半角OnlyのUTF16でもなさそうなのでバイナリ
                        return CharCode.BINARY;
                    }
                }
                return CharCode.UTF16BE;
            }
            if (b2 == 0x00 && (datasize % 2 == 0))
            {
                for (int i = 0; i < datasize; i = i + 2)
                {
                    if (data[i] < 0x06 || data[i] >= 0x7f || data[i + 1] != 0x00)
                    {   //半角OnlyのUTF16でもなさそうなのでバイナリ
                        return CharCode.BINARY;
                    }
                }
                return CharCode.UTF16LE;
            }

            //全バイト内容を走査・まずAscii,JIS判定
            int pos = 0;
            int jisCount = 0;
            while (pos < datasize)
            {
                b1 = data[pos];
                if (b1 < 0x03 || b1 >= 0x7f)
                {   //非ascii(UTF,SJis等)発見：次のループへ
                    break;
                }
                else if (b1 == 0x1b)
                {   //ESC(JIS)判定
                    //2バイト目以降の値を把握
                    b2 = ((pos < datasize + 1) ? data[pos + 1] : (byte)0);
                    b3 = ((pos < datasize + 2) ? data[pos + 2] : (byte)0);
                    b4 = ((pos < datasize + 3) ? data[pos + 3] : (byte)0);
                    //B2の値をもとに判定
                    if (b2 == 0x24)
                    {   //ESC$
                        if (b3 == 0x40 || b3 == 0x42)
                        {   //ESC $@,$B : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                        else if (b3 == 0x28 && (b4 == 0x44 || b4 == 0x4F || b4 == 0x51 || b4 == 0x50))
                        {   //ESC$(D, ESC$(O, ESC$(Q, ESC$(P : JISエスケープ
                            jisCount++;
                            pos = pos + 3;
                        }
                    }
                    else if (b2 == 0x26)
                    {   //ESC& : JISエスケープ
                        if (b3 == 0x40)
                        {   //ESC &@ : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                    }
                    else if (b2 == 0x28)
                    {   //ESC((28)
                        if (b3 == 0x4A || b3 == 0x49 || b3 == 0x42)
                        {   //ESC(J, ESC(I, ESC(B : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                    }
                }
                pos++;
            }
            //Asciiのみならここで文字コード決定
            if (pos == datasize)
            {
                if (jisCount > 0)
                {   //JIS出現
                    return CharCode.JIS;
                }
                else
                {   //JIS未出現。Ascii
                    return CharCode.ASCII;
                }
            }

            bool prevIsKanji = false; //文字コード判定強化、同種文字のときにポイント加算-HNXgrep
            int notAsciiPos = pos;
            int utfCount = 0;
            //UTF妥当性チェック（バイナリ判定を行いながら実施）
            while (pos < datasize)
            {
                b1 = data[pos];
                pos++;

                if (b1 < 0x03 || b1 == 0x7f || b1 == 0xff)
                {   //バイナリ文字：直接脱出
                    return CharCode.BINARY;
                }
                if (b1 < 0x80 || utfCount < 0)
                {   //半角文字・非UTF確定時は、後続処理は行わない
                    continue; // 半角文字は特にチェックしない
                }

                //2バイト目を把握、コードチェック
                b2 = ((pos < datasize) ? data[pos] : (byte)0x00);
                if (b1 < 0xC2 || b1 >= 0xf5)
                {   //１バイト目がC0,C1,F5以降、または２バイト目にしか現れないはずのコードが出現、NG
                    utfCount = -1;
                }
                else if (b1 < 0xe0)
                {   //2バイト文字：コードチェック
                    if (b2 >= 0x80 && b2 <= 0xbf)
                    {   //２バイト目に現れるべきコードが出現、OK（半角文字として扱う）
                        if (prevIsKanji == false) { utfCount += 2; } else { utfCount += 1; prevIsKanji = false; }
                        pos++;
                    }
                    else
                    {   //２バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
                else if (b1 < 0xf0)
                {   //3バイト文字：３バイト目を把握
                    b3 = ((pos + 1 < datasize) ? data[pos + 1] : (byte)0x00);
                    if (b2 >= 0x80 && b2 <= 0xbf && b3 >= 0x80 && b3 <= 0xbf)
                    {   //２/３バイト目に現れるべきコードが出現、OK（全角文字扱い）
                        if (prevIsKanji == true) { utfCount += 4; } else { utfCount += 3; prevIsKanji = true; }
                        pos += 2;
                    }
                    else
                    {   //２/３バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
                else
                {   //４バイト文字：３，４バイト目を把握
                    b3 = ((pos + 1 < datasize) ? data[pos + 1] : (byte)0x00);
                    b4 = ((pos + 2 < datasize) ? data[pos + 2] : (byte)0x00);
                    if (b2 >= 0x80 && b2 <= 0xbf && b3 >= 0x80 && b3 <= 0xbf && b4 >= 0x80 && b4 <= 0xbf)
                    {   //２/３/４バイト目に現れるべきコードが出現、OK（全角文字扱い）
                        if (prevIsKanji == true) { utfCount += 6; } else { utfCount += 4; prevIsKanji = true; }
                        pos += 3;
                    }
                    else
                    {   //２/３/４バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
            }

            //SJIS妥当性チェック
            pos = notAsciiPos;
            int sjisCount = 0;
            while (sjisCount >= 0 && pos < datasize)
            {
                b1 = data[pos];
                pos++;
                if (b1 < 0x80) { continue; }// 半角文字は特にチェックしない
                else if (b1 == 0x80 || b1 == 0xA0 || b1 >= 0xFD)
                {   //SJISコード外、可能性を破棄
                    sjisCount = -1;
                }
                else if ((b1 > 0x80 && b1 < 0xA0) || b1 > 0xDF)
                {   //全角文字チェックのため、2バイト目の値を把握
                    b2 = ((pos < datasize) ? data[pos] : (byte)0x00);
                    //全角文字範囲外じゃないかチェック
                    if (b2 < 0x40 || b2 == 0x7f || b2 > 0xFC)
                    {   //可能性を除外
                        sjisCount = -1;
                    }
                    else
                    {   //全角文字数を加算,ポジションを進めておく
                        if (prevIsKanji == true) { sjisCount += 2; } else { sjisCount += 1; prevIsKanji = true; }
                        pos++;
                    }
                }
                else if (prevIsKanji == false)
                {
                    //半角文字数の加算（半角カナの連続はボーナス点を高めに）
                    sjisCount += 1;
                }
                else
                {
                    prevIsKanji = false;
                }
            }
            //EUC妥当性チェック
            pos = notAsciiPos;
            int eucCount = 0;
            while (eucCount >= 0 && pos < datasize)
            {
                b1 = data[pos];
                pos++;
                if (b1 < 0x80) { continue; } // 半角文字は特にチェックしない
                //2バイト目を把握、コードチェック
                b2 = ((pos < datasize) ? data[pos] : (byte)0);
                if (b1 == 0x8e)
                {   //1バイト目＝かな文字指定。2バイトの半角カナ文字チェック
                    if (b2 < 0xA1 || b2 > 0xdf)
                    {   //可能性破棄
                        eucCount = -1;
                    }
                    else
                    {   //検出OK,EUC文字数を加算（半角文字）
                        if (prevIsKanji == false) { eucCount += 2; } else { eucCount += 1; prevIsKanji = false; }
                        pos++;
                    }
                }
                else if (b1 == 0x8f)
                {   //１バイト目の値＝３バイト文字を指定
                    if (b2 < 0xa1 || (pos + 1 < datasize && data[pos + 1] < 0xa1))
                    {   //２バイト目・３バイト目で可能性破棄
                        eucCount = -1;
                    }
                    else
                    {   //検出OK,EUC文字数を加算（全角文字）
                        if (prevIsKanji == true) { eucCount += 3; } else { eucCount += 1; prevIsKanji = true; }
                        pos += 2;
                    }
                }
                else if (b1 < 0xa1 || b2 < 0xa1)
                {   //２バイト文字のはずだったがどちらかのバイトがNG
                    eucCount = -1;
                }
                else
                {   //２バイト文字OK（全角）
                    if (prevIsKanji == true) { eucCount += 2; } else { eucCount += 1; prevIsKanji = true; }
                    pos++;
                }
            }

            //文字コード決定
            if (eucCount > sjisCount && eucCount > utfCount)
            {
                return CharCode.EUC;
            }
            else if (utfCount > sjisCount)
            {
                return CharCode.UTF8N;
            }
            else if (sjisCount > -1)
            {
                return CharCode.SJIS;
            }
            else
            {
                return CharCode.BINARY;
            }
        }
    }
}