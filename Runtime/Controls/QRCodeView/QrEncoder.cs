/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// How much of a QR code can be damaged and still read: about 7, 15, 25 and 30 percent.
    /// </summary>
    public enum QrErrorCorrection
    {
        Low,
        Medium,
        Quartile,
        High,
    }

    /// <summary>
    /// An encoded QR code: a square grid of dark and light modules, without the quiet zone.
    /// </summary>
    public sealed class QrCode
    {
        private readonly bool[,] modules;

        internal QrCode(int version, QrErrorCorrection errorCorrection, int mask, bool[,] modules)
        {
            Version = version;
            ErrorCorrection = errorCorrection;
            Mask = mask;
            this.modules = modules;
        }

        /// <summary>
        /// The symbol version, 1 to 40.
        /// </summary>
        public int Version { get; }

        public QrErrorCorrection ErrorCorrection { get; }

        /// <summary>
        /// The mask pattern applied, 0 to 7.
        /// </summary>
        public int Mask { get; }

        /// <summary>
        /// Modules per side: 21 for version 1, plus 4 per version.
        /// </summary>
        public int Size => modules.GetLength(0);

        /// <summary>
        /// True when the module at column <paramref name="x"/>, row <paramref name="y"/> is dark. Row 0 is the top.
        /// </summary>
        public bool this[int x, int y] => modules[y, x];
    }

    /// <summary>
    /// A self-contained QR code encoder (ISO/IEC 18004, model 2). Picks numeric, alphanumeric or byte (UTF-8) mode,
    /// the smallest version that fits, and the mask with the lowest penalty.
    /// </summary>
    /// <remarks>
    /// <para>No Unity types, so it runs anywhere. <see cref="QRCodeView"/> draws the result.
    /// The block tables and the alignment pattern formula follow the layout used by Project Nayuki's
    /// QR Code generator (MIT).</para>
    /// </remarks>
    public static class QrEncoder
    {
        public const int MinVersion = 1;
        public const int MaxVersion = 40;

        private const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

        // Error correction codewords per block, by level then version. Index 0 is unused.
        private static readonly int[][] EccCodewordsPerBlock =
        {
            new[] { -1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            new[] { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
            new[] { -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            new[] { -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        };

        // Error correction blocks, by level then version. Index 0 is unused.
        private static readonly int[][] NumErrorCorrectionBlocks =
        {
            new[] { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
            new[] { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
            new[] { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
            new[] { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 },
        };

        private enum Mode
        {
            Numeric,
            Alphanumeric,
            Byte,
        }

        /// <summary>
        /// Encodes text. Throws <see cref="ArgumentException"/> when it does not fit in <paramref name="maxVersion"/>.
        /// </summary>
        /// <param name="text">The content, such as a URL. Null encodes an empty code.</param>
        /// <param name="errorCorrection">The minimum level; the result never uses less.</param>
        /// <param name="mask">A mask from 0 to 7, or -1 to pick the one with the lowest penalty.</param>
        public static QrCode Encode(string text, QrErrorCorrection errorCorrection = QrErrorCorrection.Medium, int minVersion = MinVersion, int maxVersion = MaxVersion, int mask = -1)
        {
            if (minVersion < MinVersion || maxVersion > MaxVersion || minVersion > maxVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(minVersion), "Versions run from 1 to 40.");
            }

            if (mask < -1 || mask > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(mask), "Masks run from 0 to 7, or -1 for automatic.");
            }

            text ??= string.Empty;
            var mode = ChooseMode(text);
            var payload = mode == Mode.Byte ? Encoding.UTF8.GetBytes(text) : null;
            var count = mode == Mode.Byte ? payload.Length : text.Length;

            var version = minVersion;
            int dataBitsNeeded;
            while (true)
            {
                dataBitsNeeded = 4 + CharCountBits(mode, version) + PayloadBits(mode, count);
                if (CharCountFits(mode, version, count) && dataBitsNeeded <= NumDataCodewords(version, errorCorrection) * 8)
                {
                    break;
                }

                if (version >= maxVersion)
                {
                    throw new ArgumentException($"The text is too long for a QR code up to version {maxVersion}.", nameof(text));
                }

                version++;
            }

            var bits = new BitBuffer();
            bits.Append(mode switch { Mode.Numeric => 0x1, Mode.Alphanumeric => 0x2, _ => 0x4 }, 4);
            bits.Append(count, CharCountBits(mode, version));
            switch (mode)
            {
                case Mode.Numeric:
                    for (var i = 0; i < text.Length; i += 3)
                    {
                        var length = Math.Min(3, text.Length - i);
                        bits.Append(int.Parse(text.Substring(i, length)), length * 3 + 1);
                    }

                    break;
                case Mode.Alphanumeric:
                    for (var i = 0; i < text.Length; i += 2)
                    {
                        if (i + 1 < text.Length)
                        {
                            bits.Append(AlphanumericCharset.IndexOf(text[i]) * 45 + AlphanumericCharset.IndexOf(text[i + 1]), 11);
                        }
                        else
                        {
                            bits.Append(AlphanumericCharset.IndexOf(text[i]), 6);
                        }
                    }

                    break;
                default:
                    foreach (var b in payload)
                    {
                        bits.Append(b, 8);
                    }

                    break;
            }

            // Terminator, byte alignment, then alternating pad bytes.
            var capacityBits = NumDataCodewords(version, errorCorrection) * 8;
            bits.Append(0, Math.Min(4, capacityBits - bits.Count));
            bits.Append(0, (8 - bits.Count % 8) % 8);
            for (var pad = 0xEC; bits.Count < capacityBits; pad ^= 0xEC ^ 0x11)
            {
                bits.Append(pad, 8);
            }

            var codewords = AddEccAndInterleave(bits.ToBytes(), version, errorCorrection);
            var matrix = new Matrix(version);
            matrix.DrawFunctionPatterns();
            matrix.DrawCodewords(codewords);

            if (mask == -1)
            {
                var best = int.MaxValue;
                for (var candidate = 0; candidate < 8; candidate++)
                {
                    matrix.ApplyMask(candidate);
                    matrix.DrawFormatBits(errorCorrection, candidate);
                    var penalty = matrix.Penalty();
                    if (penalty < best)
                    {
                        best = penalty;
                        mask = candidate;
                    }

                    // XOR again to undo the mask.
                    matrix.ApplyMask(candidate);
                }
            }

            matrix.ApplyMask(mask);
            matrix.DrawFormatBits(errorCorrection, mask);
            return new QrCode(version, errorCorrection, mask, matrix.Modules);
        }

        /// <summary>
        /// The number of data codewords (bytes) a version holds at a level, after error correction.
        /// </summary>
        public static int NumDataCodewords(int version, QrErrorCorrection errorCorrection) =>
            NumRawDataModules(version) / 8 - EccCodewordsPerBlock[(int)errorCorrection][version] * NumErrorCorrectionBlocks[(int)errorCorrection][version];

        /// <summary>
        /// The centre coordinates of the alignment patterns, ascending; empty for version 1.
        /// </summary>
        public static int[] AlignmentPatternPositions(int version)
        {
            if (version == 1)
            {
                return Array.Empty<int>();
            }

            var count = version / 7 + 2;
            var step = (version * 8 + count * 3 + 5) / (count * 4 - 4) * 2;
            var result = new int[count];
            result[0] = 6;
            for (int i = count - 1, position = version * 4 + 17 - 7; i >= 1; i--, position -= step)
            {
                result[i] = position;
            }

            return result;
        }

        private static Mode ChooseMode(string text)
        {
            var numeric = true;
            var alphanumeric = true;
            foreach (var c in text)
            {
                numeric &= c >= '0' && c <= '9';
                alphanumeric &= AlphanumericCharset.IndexOf(c) >= 0;
            }

            return text.Length > 0 && numeric ? Mode.Numeric : text.Length > 0 && alphanumeric ? Mode.Alphanumeric : Mode.Byte;
        }

        private static int CharCountBits(Mode mode, int version)
        {
            var band = version <= 9 ? 0 : version <= 26 ? 1 : 2;
            return mode switch
            {
                Mode.Numeric => new[] { 10, 12, 14 }[band],
                Mode.Alphanumeric => new[] { 9, 11, 13 }[band],
                _ => new[] { 8, 16, 16 }[band],
            };
        }

        private static bool CharCountFits(Mode mode, int version, int count) => count < 1 << CharCountBits(mode, version);

        private static int PayloadBits(Mode mode, int count) => mode switch
        {
            Mode.Numeric => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 * 3 + 1),
            Mode.Alphanumeric => count / 2 * 11 + count % 2 * 6,
            _ => count * 8,
        };

        // Modules left for data and error correction once the function patterns and format bits are placed.
        private static int NumRawDataModules(int version)
        {
            var result = (16 * version + 128) * version + 64;
            if (version >= 2)
            {
                var alignments = version / 7 + 2;
                result -= (25 * alignments - 10) * alignments - 55;
                if (version >= 7)
                {
                    result -= 36;
                }
            }

            return result;
        }

        private static byte[] AddEccAndInterleave(byte[] data, int version, QrErrorCorrection errorCorrection)
        {
            var numBlocks = NumErrorCorrectionBlocks[(int)errorCorrection][version];
            var blockEccLength = EccCodewordsPerBlock[(int)errorCorrection][version];
            var rawCodewords = NumRawDataModules(version) / 8;
            var numShortBlocks = numBlocks - rawCodewords % numBlocks;
            var shortBlockLength = rawCodewords / numBlocks;

            // Every block is stored at the long length; short blocks carry one unused byte before their ECC.
            var divisor = ReedSolomonDivisor(blockEccLength);
            var blocks = new byte[numBlocks][];
            for (int i = 0, offset = 0; i < numBlocks; i++)
            {
                var dataLength = shortBlockLength - blockEccLength + (i < numShortBlocks ? 0 : 1);
                var block = new byte[shortBlockLength + 1];
                Array.Copy(data, offset, block, 0, dataLength);
                var dataPart = new byte[dataLength];
                Array.Copy(data, offset, dataPart, 0, dataLength);
                offset += dataLength;
                var ecc = ReedSolomonRemainder(dataPart, divisor);
                Array.Copy(ecc, 0, block, block.Length - blockEccLength, blockEccLength);
                blocks[i] = block;
            }

            var result = new byte[rawCodewords];
            var k = 0;
            for (var i = 0; i < shortBlockLength + 1; i++)
            {
                for (var j = 0; j < numBlocks; j++)
                {
                    if (i != shortBlockLength - blockEccLength || j >= numShortBlocks)
                    {
                        result[k++] = blocks[j][i];
                    }
                }
            }

            return result;
        }

        private static byte[] ReedSolomonDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            var root = 1;
            for (var i = 0; i < degree; i++)
            {
                for (var j = 0; j < degree; j++)
                {
                    result[j] = (byte)GfMultiply(result[j], root);
                    if (j + 1 < degree)
                    {
                        result[j] ^= result[j + 1];
                    }
                }

                root = GfMultiply(root, 0x02);
            }

            return result;
        }

        private static byte[] ReedSolomonRemainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            foreach (var b in data)
            {
                var factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] ^= (byte)GfMultiply(divisor[i], factor);
                }
            }

            return result;
        }

        // Multiplication in GF(2^8) modulo x^8 + x^4 + x^3 + x^2 + 1.
        private static int GfMultiply(int x, int y)
        {
            var z = 0;
            for (var i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }

            return z;
        }

        private sealed class BitBuffer
        {
            private readonly List<bool> bits = new List<bool>();

            public int Count => bits.Count;

            public void Append(int value, int length)
            {
                for (var i = length - 1; i >= 0; i--)
                {
                    bits.Add(((value >> i) & 1) != 0);
                }
            }

            public byte[] ToBytes()
            {
                var result = new byte[bits.Count / 8];
                for (var i = 0; i < result.Length * 8; i++)
                {
                    if (bits[i])
                    {
                        result[i >> 3] |= (byte)(0x80 >> (i & 7));
                    }
                }

                return result;
            }
        }

        private sealed class Matrix
        {
            private readonly int version;
            private readonly int size;
            private readonly bool[,] isFunction;

            public Matrix(int version)
            {
                this.version = version;
                size = version * 4 + 17;
                Modules = new bool[size, size];
                isFunction = new bool[size, size];
            }

            public bool[,] Modules { get; }

            public void DrawFunctionPatterns()
            {
                for (var i = 0; i < size; i++)
                {
                    Set(6, i, i % 2 == 0);
                    Set(i, 6, i % 2 == 0);
                }

                DrawFinder(3, 3);
                DrawFinder(size - 4, 3);
                DrawFinder(3, size - 4);

                var positions = AlignmentPatternPositions(version);
                var last = positions.Length - 1;
                for (var i = 0; i < positions.Length; i++)
                {
                    for (var j = 0; j < positions.Length; j++)
                    {
                        // The three corners hold finder patterns.
                        if ((i == 0 && j == 0) || (i == 0 && j == last) || (i == last && j == 0))
                        {
                            continue;
                        }

                        for (var dy = -2; dy <= 2; dy++)
                        {
                            for (var dx = -2; dx <= 2; dx++)
                            {
                                Set(positions[i] + dx, positions[j] + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                            }
                        }
                    }
                }

                // Reserve the format areas; the real bits are drawn once the mask is known.
                DrawFormatBits(QrErrorCorrection.Medium, 0);
                DrawVersionBits();
            }

            public void DrawFormatBits(QrErrorCorrection errorCorrection, int mask)
            {
                var levelBits = errorCorrection switch
                {
                    QrErrorCorrection.Low => 1,
                    QrErrorCorrection.Medium => 0,
                    QrErrorCorrection.Quartile => 3,
                    _ => 2,
                };
                var data = levelBits << 3 | mask;
                var remainder = data;
                for (var i = 0; i < 10; i++)
                {
                    remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
                }

                var bits = (data << 10 | remainder) ^ 0x5412;

                // The copy around the top-left finder.
                for (var i = 0; i <= 5; i++)
                {
                    Set(8, i, Bit(bits, i));
                }

                Set(8, 7, Bit(bits, 6));
                Set(8, 8, Bit(bits, 7));
                Set(7, 8, Bit(bits, 8));
                for (var i = 9; i < 15; i++)
                {
                    Set(14 - i, 8, Bit(bits, i));
                }

                // The copy split between the other two finders.
                for (var i = 0; i < 8; i++)
                {
                    Set(size - 1 - i, 8, Bit(bits, i));
                }

                for (var i = 8; i < 15; i++)
                {
                    Set(8, size - 15 + i, Bit(bits, i));
                }

                // The module that is always dark.
                Set(8, size - 8, true);
            }

            public void DrawCodewords(byte[] data)
            {
                var i = 0;
                for (var right = size - 1; right >= 1; right -= 2)
                {
                    // Skip the vertical timing column.
                    if (right == 6)
                    {
                        right = 5;
                    }

                    for (var vert = 0; vert < size; vert++)
                    {
                        for (var j = 0; j < 2; j++)
                        {
                            var x = right - j;
                            var upward = ((right + 1) & 2) == 0;
                            var y = upward ? size - 1 - vert : vert;
                            if (!isFunction[y, x] && i < data.Length * 8)
                            {
                                Modules[y, x] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                                i++;
                            }
                        }
                    }
                }
            }

            public void ApplyMask(int mask)
            {
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        if (isFunction[y, x])
                        {
                            continue;
                        }

                        var invert = mask switch
                        {
                            0 => (x + y) % 2 == 0,
                            1 => y % 2 == 0,
                            2 => x % 3 == 0,
                            3 => (x + y) % 3 == 0,
                            4 => (x / 3 + y / 2) % 2 == 0,
                            5 => x * y % 2 + x * y % 3 == 0,
                            6 => (x * y % 2 + x * y % 3) % 2 == 0,
                            _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                        };
                        Modules[y, x] ^= invert;
                    }
                }
            }

            // The four penalty rules from the specification; lower is easier to read.
            public int Penalty()
            {
                var result = 0;
                var dark = 0;
                for (var a = 0; a < size; a++)
                {
                    var rowRun = 1;
                    var columnRun = 1;
                    for (var b = 0; b < size; b++)
                    {
                        if (Modules[a, b])
                        {
                            dark++;
                        }

                        if (b > 0)
                        {
                            rowRun = Modules[a, b] == Modules[a, b - 1] ? rowRun + 1 : 1;
                            columnRun = Modules[b, a] == Modules[b - 1, a] ? columnRun + 1 : 1;
                            result += rowRun == 5 ? 3 : rowRun > 5 ? 1 : 0;
                            result += columnRun == 5 ? 3 : columnRun > 5 ? 1 : 0;
                        }

                        if (a > 0 && b > 0)
                        {
                            var colour = Modules[a, b];
                            if (colour == Modules[a - 1, b] && colour == Modules[a, b - 1] && colour == Modules[a - 1, b - 1])
                            {
                                result += 3;
                            }
                        }

                        if (b + 10 < size)
                        {
                            if (IsFinderLike(i => Modules[a, b + i]))
                            {
                                result += 40;
                            }

                            if (IsFinderLike(i => Modules[b + i, a]))
                            {
                                result += 40;
                            }
                        }
                    }
                }

                var total = size * size;
                var k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
                return result + k * 10;
            }

            // 1:1:3:1:1 dark-light-dark-light-dark with four light modules on one side.
            private static bool IsFinderLike(Func<int, bool> at)
            {
                bool Core(int o) => at(o) && !at(o + 1) && at(o + 2) && at(o + 3) && at(o + 4) && !at(o + 5) && at(o + 6);
                bool Light(int o) => !at(o) && !at(o + 1) && !at(o + 2) && !at(o + 3);
                return (Core(0) && Light(7)) || (Light(0) && Core(4));
            }

            private void DrawFinder(int cx, int cy)
            {
                for (var dy = -4; dy <= 4; dy++)
                {
                    for (var dx = -4; dx <= 4; dx++)
                    {
                        var x = cx + dx;
                        var y = cy + dy;
                        if (x < 0 || x >= size || y < 0 || y >= size)
                        {
                            continue;
                        }

                        var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        Set(x, y, distance != 2 && distance != 4);
                    }
                }
            }

            private void DrawVersionBits()
            {
                if (version < 7)
                {
                    return;
                }

                var remainder = version;
                for (var i = 0; i < 12; i++)
                {
                    remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
                }

                var bits = version << 12 | remainder;
                for (var i = 0; i < 18; i++)
                {
                    var bit = Bit(bits, i);
                    var a = size - 11 + i % 3;
                    var b = i / 3;
                    Set(a, b, bit);
                    Set(b, a, bit);
                }
            }

            private void Set(int x, int y, bool dark)
            {
                Modules[y, x] = dark;
                isFunction[y, x] = true;
            }

            private static bool Bit(int value, int index) => ((value >> index) & 1) != 0;
        }
    }
}
