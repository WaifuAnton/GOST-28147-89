using System;

namespace Lab9_Csh
{
    class Gost
    {
        const int BLOCK_SIZE_BYTES = 8;

        readonly byte[,] s =
        {
            { 4, 10, 9, 2, 13, 8, 0, 14, 6, 11, 1, 12, 7, 15, 5, 3 },
            { 14, 11, 4, 12, 6, 13, 15, 10, 2, 3, 8, 1, 0, 7, 5, 9 },
            { 5, 8, 1, 13, 10, 3, 4, 2, 14, 15, 12, 7, 6, 0, 9, 11 },
            { 7, 13, 10, 1, 0, 8, 9, 15, 14, 4, 6, 12, 11, 2, 5, 3 },
            { 6, 12, 7, 1, 5, 15, 13, 8, 4, 10, 9, 14, 0, 3, 11, 2 },
            { 4, 11, 10, 0, 7, 2, 1, 13, 3, 6, 8, 5, 9, 12, 15, 14 },
            { 13, 11, 4, 1, 3, 15, 5, 9, 0, 10, 14, 7, 6, 8, 2, 12 },
            { 1, 15, 13, 0, 5, 7, 10, 4, 9, 2, 3, 14, 6, 11, 8, 12 }
        };

        private uint[] key = new uint[8];
        private byte[] synhropossylkaNapysanaTranslitomBoMeniTakHochetsia;

        private int gammaStop = 0;
        private int reverseGammaStop = 0;
        private byte[][] gammas;

        public Gost(uint[] key, byte[] synhropossylkaNapysanaTranslitomBoMeniTakHochetsia)
        {
            this.key = key;
            this.synhropossylkaNapysanaTranslitomBoMeniTakHochetsia = synhropossylkaNapysanaTranslitomBoMeniTakHochetsia;
        }

        private uint Substitute(uint value)
        {
            byte index, sBlock;
            uint result = 0;
            for (int i = 0; i < 8; i++)
            {
                index = (byte)(value >> (4 * i) & 0x0f);
                sBlock = s[i, index];
                result |= (uint)sBlock << (4 * i);
            }
            return result;
        }

        private uint F(uint block, uint subKey)
        {
            block = (block + subKey) % uint.MaxValue;
            block = Substitute(block);
            block = (block << 11) | (block >> 21);
            return block;
        }

        private byte[] Encrypt(byte[] inputBlock)
        {
            uint left = BitConverter.ToUInt32(inputBlock, 0);
            uint right = BitConverter.ToUInt32(inputBlock, 4);
            byte[] result = new byte[BLOCK_SIZE_BYTES];
            for (int i = 0; i < 24; i++)
            {
                uint fResult = left ^ F(right, key[i % 8]);
                left = right;
                right = fResult;
            }
            for (int i = 24, j = 7; i < 31; i++, j--)
            {
                uint fResult = left ^ F(right, key[j]);
                left = right;
                right = fResult;
            }
            left ^= F(right, key[0]);
            Array.Copy(BitConverter.GetBytes(left), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(right), 0, result, 4, 4);
            return result;
        }

        private byte[] Decrypt(byte[] inputBlock)
        {
            uint left = BitConverter.ToUInt32(inputBlock, 0);
            uint right = BitConverter.ToUInt32(inputBlock, 4);
            byte[] result = new byte[BLOCK_SIZE_BYTES];
            for (int i = 31, j = 0; i >= 24; i--, j++)
            {
                uint fResult = left ^ F(right, key[j]);
                left = right;
                right = fResult;
            }
            for (int i = 23; i > 0; i--)
            {
                uint fResult = left ^ F(right, key[i % 8]);
                left = right;
                right = fResult;
            }
            left ^= F(right, key[0]);
            Array.Copy(BitConverter.GetBytes(left), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(right), 0, result, 4, 4);
            return result;
        }

        public byte[] EncryptECB(byte[] input)
        {
            if (input.Length % BLOCK_SIZE_BYTES != 0)
                throw new ArgumentException("Data size isn't multiple of block size");
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            for (int i = 0; i < size; i++)
                outputBlocks[i] = Encrypt(inputBlocks[i]);
            return ConcatData(outputBlocks);
        }

        public byte[] DecryptECB(byte[] input)
        {
            if (input.Length % BLOCK_SIZE_BYTES != 0)
                throw new ArgumentException("Data size isn't multiple of block size");
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            for (int i = 0; i < size; i++)
                outputBlocks[i] = Decrypt(inputBlocks[i]);
            return ConcatData(outputBlocks);
        }

        public byte[] EncryptGamma(byte[] input)
        {
            gammaStop = input.Length;
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            gammas = new byte[size][];
            gammas[0] = GenerateSingleGamma(synhropossylkaNapysanaTranslitomBoMeniTakHochetsia);
            for (int i = 1; i < gammas.Length; i++)
                gammas[i] = GenerateSingleGamma(gammas[i - 1]);
            for (int i = 0; i < size; i++)
                outputBlocks[i] = XOR(gammas[i], inputBlocks[i]);
            return ConcatData(outputBlocks);
        }

        public byte[] DecryptGamma(byte[] input)
        {
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            byte[][] gamma = new byte[size][];
            gamma[0] = GenerateSingleGamma(synhropossylkaNapysanaTranslitomBoMeniTakHochetsia);
            for (int i = 1; i < gamma.Length; i++)
                gamma[i] = GenerateSingleGamma(gamma[i - 1]);
            for (int i = 0; i < size; i++)
                outputBlocks[i] = XOR(gamma[i], inputBlocks[i]);
            byte[] temp = ConcatData(outputBlocks);
            byte[] output = new byte[gammaStop];
            Array.Copy(temp, output, gammaStop);
            return output;
        }

        public byte[] EncryptReverseGamma(byte[] input)
        {
            reverseGammaStop = input.Length;
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            byte[] gamma = Encrypt(synhropossylkaNapysanaTranslitomBoMeniTakHochetsia);
            outputBlocks[0] = XOR(gamma, inputBlocks[0]);
            for (int i = 1; i < size; i++)
                outputBlocks[i] = XOR(Encrypt(outputBlocks[i - 1]), inputBlocks[i]);
            return ConcatData(outputBlocks);
        }

        public byte[] DecryptReverseGamma(byte[] input)
        {
            int size = BlocksCeiling(input);
            byte[][] inputBlocks = SplitData(input);
            byte[][] outputBlocks = new byte[size][];
            byte[] gamma = Encrypt(synhropossylkaNapysanaTranslitomBoMeniTakHochetsia);
            outputBlocks[0] = XOR(gamma, inputBlocks[0]);
            for (int i = 1; i < size; i++)
                outputBlocks[i] = XOR(Encrypt(inputBlocks[i - 1]), inputBlocks[i]);
            byte[] temp = ConcatData(outputBlocks);
            byte[] output = new byte[reverseGammaStop];
            Array.Copy(temp, output, reverseGammaStop);
            return output;
        }

        public byte[] GenerateSingleGamma(byte[] input)
        {
            byte[] zashifrovanaSsylka = Encrypt(input);
            uint leftSsylka = BitConverter.ToUInt32(zashifrovanaSsylka, 0);
            uint rightSsylka = BitConverter.ToUInt32(zashifrovanaSsylka, 4);
            leftSsylka = (leftSsylka + 0x1010101) % uint.MaxValue;
            rightSsylka = (rightSsylka + 0x1010103) % uint.MaxValue - 1;
            rightSsylka++;
            byte[] result = new byte[8];
            Array.Copy(BitConverter.GetBytes(leftSsylka), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(rightSsylka), 0, result, 4, 4);
            return result;
        }

        private byte[][] SplitData(byte[] input)
        {
            int size = BlocksCeiling(input);
            byte[][] output = new byte[size][];
            for (int i = 0; i < size; i++)
                output[i] = new byte[BLOCK_SIZE_BYTES];
            for (int i = 0, j = 0, k = 0; k < input.Length; k++, j++)
            {
                if (j == BLOCK_SIZE_BYTES)
                {
                    i++;
                    j = 0;
                }
                output[i][j] = input[k];
            }
            return output;
        }

        private byte[] ConcatData(byte[][] input)
        {
            byte[] output = new byte[input.Length * BLOCK_SIZE_BYTES];
            for (int i = 0, j = 0, k = 0; k < output.Length; k++, j++)
            {
                if (j == BLOCK_SIZE_BYTES)
                {
                    i++;
                    j = 0;
                }
                output[k] = input[i][j];
            }
            return output;
        }

        private int BlocksCeiling(byte[] input)
        {
            int result = input.Length / BLOCK_SIZE_BYTES;
            if (input.Length % BLOCK_SIZE_BYTES != 0)
                result++;
            return result;
        }

        private byte[] XOR(byte[] left, byte[] right)
        {
            byte[] result = new byte[BLOCK_SIZE_BYTES];
            for (int i = 0; i < result.Length; i++)
                result[i] = (byte)(left[i] ^ right[i]);
            return result;
        }

        public byte[][] GetGammas()
        {
            return gammas;
        }
    }
}
