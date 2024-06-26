//************************************************************************************
// BigInteger Class Version 1.03
//
// Copyright (c) 2002 Chew Keong TAN
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, provided that the above
// copyright notice(s) and this permission notice appear in all copies of
// the Software and that both the above copyright notice(s) and this
// permission notice appear in supporting documentation.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
// OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL
// INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING
// FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
// NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
// WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//
//
// Disclaimer
// ----------
// Although reasonable care has been taken to ensure the correctness of this
// implementation, this code should never be used in any application without
// proper verification and testing.  I disclaim all liability and responsibility
// to any person or entity with respect to any loss or damage caused, or alleged
// to be caused, directly or indirectly, by the use of this BigInteger class.
//
// Comments, bugs and suggestions to
// (http://www.codeproject.com/csharp/biginteger.asp)
//
//
// Overloaded Operators +, -, *, /, %, >>, <<, ==, !=, >, <, >=, <=, &, |, ^, ++, --, ~
//
// Features
// --------
// 1) Arithmetic operations involving large signed integers (2's complement).
// 2) Primality test using Fermat little theorm, Rabin Miller's method,
//    Solovay Strassen's method and Lucas strong pseudoprime.
// 3) Modulo exponential with Barrett's reduction.
// 4) Inverse modulo.
// 5) Pseudo prime generation.
// 6) Co-prime generation.
//
//
// Known Problem
// -------------
// This pseudoprime passes my implementation of
// primality test but failed in JDK's isProbablePrime test.
//
//       byte[] pseudoPrime1 = { (byte)0x00,
//             (byte)0x85, (byte)0x84, (byte)0x64, (byte)0xFD, (byte)0x70, (byte)0x6A,
//             (byte)0x9F, (byte)0xF0, (byte)0x94, (byte)0x0C, (byte)0x3E, (byte)0x2C,
//             (byte)0x74, (byte)0x34, (byte)0x05, (byte)0xC9, (byte)0x55, (byte)0xB3,
//             (byte)0x85, (byte)0x32, (byte)0x98, (byte)0x71, (byte)0xF9, (byte)0x41,
//             (byte)0x21, (byte)0x5F, (byte)0x02, (byte)0x9E, (byte)0xEA, (byte)0x56,
//             (byte)0x8D, (byte)0x8C, (byte)0x44, (byte)0xCC, (byte)0xEE, (byte)0xEE,
//             (byte)0x3D, (byte)0x2C, (byte)0x9D, (byte)0x2C, (byte)0x12, (byte)0x41,
//             (byte)0x1E, (byte)0xF1, (byte)0xC5, (byte)0x32, (byte)0xC3, (byte)0xAA,
//             (byte)0x31, (byte)0x4A, (byte)0x52, (byte)0xD8, (byte)0xE8, (byte)0xAF,
//             (byte)0x42, (byte)0xF4, (byte)0x72, (byte)0xA1, (byte)0x2A, (byte)0x0D,
//             (byte)0x97, (byte)0xB1, (byte)0x31, (byte)0xB3,
//       };
//
//
// Change Log
// ----------
// 1) September 23, 2002 (Version 1.03)
//    - Fixed operator- to give correct data length.
//    - Added Lucas sequence generation.
//    - Added Strong Lucas Primality test.
//    - Added integer square root method.
//    - Added setBit/unsetBit methods.
//    - New isProbablePrime() method which do not require the
//      confident parameter.
//
// 2) August 29, 2002 (Version 1.02)
//    - Fixed bug in the exponentiation of negative numbers.
//    - Faster modular exponentiation using Barrett reduction.
//    - Added getBytes() method.
//    - Fixed bug in ToHexString method.
//    - Added overloading of ^ operator.
//    - Faster computation of Jacobi symbol.
//
// 3) August 19, 2002 (Version 1.01)
//    - Big integer is stored and manipulated as unsigned integers (4 bytes) instead of
//      individual bytes this gives significant performance improvement.
//    - Updated Fermat's Little Theorem test to use a^(p-1) mod p = 1
//    - Added isProbablePrime method.
//    - Updated documentation.
//
// 4) August 9, 2002 (Version 1.0)
//    - Initial Release.
//
//
// References
// [1] D. E. Knuth, "Seminumerical Algorithms", The Art of Computer Programming Vol. 2,
//     3rd Edition, Addison-Wesley, 1998.
//
// [2] K. H. Rosen, "Elementary Number Theory and Its Applications", 3rd Ed,
//     Addison-Wesley, 1993.
//
// [3] B. Schneier, "Applied Cryptography", 2nd Ed, John Wiley & Sons, 1996.
//
// [4] A. Menezes, P. van Oorschot, and S. Vanstone, "Handbook of Applied Cryptography",
//     CRC Press, 1996, www.cacr.math.uwaterloo.ca/hac
//
// [5] A. Bosselaers, R. Govaerts, and J. Vandewalle, "Comparison of Three Modular
//     Reduction Functions," Proc. CRYPTO'93, pp.175-186.
//
// [6] R. Baillie and S. S. Wagstaff Jr, "Lucas Pseudoprimes", Mathematics of Computation,
//     Vol. 35, No. 152, Oct 1980, pp. 1391-1417.
//
// [7] H. C. Williams, "�douard Lucas and Primality Testing", Canadian Mathematical
//     Society Series of Monographs and Advance Texts, vol. 22, John Wiley & Sons, New York,
//     NY, 1998.
//
// [8] P. Ribenboim, "The new book of prime number records", 3rd edition, Springer-Verlag,
//     New York, NY, 1995.
//
// [9] M. Joye and J.-J. Quisquater, "Efficient computation of full Lucas sequences",
//     Electronics Letters, 32(6), 1996, pp 537-538.
//
//************************************************************************************

using System;

public class BigInteger {
    // maximum length of the BigInteger in uint (4 bytes)
    // change this to suit the required level of precision.

    const int maxLength = 1000;

    // primes smaller than 2000 to test the generated prime number

    public static readonly int[] primesBelow2000 = {
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107,
        109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229,
        233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359,
        367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491,
        499, 503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641,
        643, 647, 653, 659, 661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787,
        797, 809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941,
        947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069,
        1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213,
        1217, 1223, 1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291, 1297, 1301, 1303, 1307, 1319, 1321,
        1327, 1361, 1367, 1373, 1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451, 1453, 1459, 1471, 1481,
        1483, 1487, 1489, 1493, 1499, 1511, 1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583, 1597, 1601,
        1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657, 1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733,
        1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789, 1801, 1811, 1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877,
        1879, 1889, 1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987, 1993, 1997, 1999
    };


    readonly uint[] data; // stores bytes from the Big Integer
    public int dataLength; // number of actual chars used


    //***********************************************************************
    // Constructor (Default value for BigInteger is 0
    //***********************************************************************

    public BigInteger() {
        data = new uint[maxLength];
        dataLength = 1;
    }


    //***********************************************************************
    // Constructor (Default value provided by long)
    //***********************************************************************

    public BigInteger(long value) {
        data = new uint[maxLength];
        var tempVal = value;

        // copy bytes from long to BigInteger without any assumption of
        // the length of the long datatype

        dataLength = 0;
        while (value != 0 && dataLength < maxLength) {
            data[dataLength] = (uint) (value & 0xFFFFFFFF);
            value >>= 32;
            dataLength++;
        }

        if (tempVal > 0) // overflow check for +ve value
        {
            if (value != 0 || (data[maxLength - 1] & 0x80000000) != 0) {
                throw new ArithmeticException("Positive overflow in constructor.");
            }
        } else if (tempVal < 0) // underflow check for -ve value
        {
            if (value != -1 || (data[dataLength - 1] & 0x80000000) == 0) {
                throw new ArithmeticException("Negative underflow in constructor.");
            }
        }

        if (dataLength == 0) {
            dataLength = 1;
        }
    }


    //***********************************************************************
    // Constructor (Default value provided by ulong)
    //***********************************************************************

    public BigInteger(ulong value) {
        data = new uint[maxLength];

        // copy bytes from ulong to BigInteger without any assumption of
        // the length of the ulong datatype

        dataLength = 0;
        while (value != 0 && dataLength < maxLength) {
            data[dataLength] = (uint) (value & 0xFFFFFFFF);
            value >>= 32;
            dataLength++;
        }

        if (value != 0 || (data[maxLength - 1] & 0x80000000) != 0) {
            throw new ArithmeticException("Positive overflow in constructor.");
        }

        if (dataLength == 0) {
            dataLength = 1;
        }
    }


    //***********************************************************************
    // Constructor (Default value provided by BigInteger)
    //***********************************************************************

    public BigInteger(BigInteger bi) {
        data = new uint[maxLength];

        dataLength = bi.dataLength;

        for (var i = 0; i < dataLength; i++) {
            data[i] = bi.data[i];
        }
    }

    //***********************************************************************
    // Constructor (Default value provided by an array of bytes)
    //
    // The lowest index of the input byte array (i.e [0]) should contain the
    // most significant byte of the number, and the highest index should
    // contain the least significant byte.
    //
    // E.g.
    // To initialize "a" with the default value of 0x1D4F in base 16
    //      byte[] temp = { 0x1D, 0x4F };
    //      BigInteger a = new BigInteger(temp)
    //
    // Note that this method of initialization does not allow the
    // sign to be specified.
    //
    //***********************************************************************

    public BigInteger(byte[] inData) {
        dataLength = inData.Length >> 2;

        var leftOver = inData.Length & 0x3;
        if (leftOver != 0) // length not multiples of 4
        {
            dataLength++;
        }


        if (dataLength > maxLength) {
            throw new ArithmeticException("Byte overflow in constructor.");
        }

        data = new uint[maxLength];

        for (int i = inData.Length - 1, j = 0; i >= 3; i -= 4, j++) {
            data[j] = (uint) ((inData[i - 3] << 24) + (inData[i - 2] << 16) + (inData[i - 1] << 8) +
                              inData[i]);
        }

        if (leftOver == 1) {
            data[dataLength - 1] = inData[0];
        } else if (leftOver == 2) {
            data[dataLength - 1] = (uint) ((inData[0] << 8) + inData[1]);
        } else if (leftOver == 3) {
            data[dataLength - 1] = (uint) ((inData[0] << 16) + (inData[1] << 8) + inData[2]);
        }


        while (dataLength > 1 && data[dataLength - 1] == 0) {
            dataLength--;
        }

        //Console.WriteLine("Len = " + dataLength);
    }

    //***********************************************************************
    // Constructor (Default value provided by an array of unsigned integers)
    //*********************************************************************

    public BigInteger(uint[] inData) {
        dataLength = inData.Length;

        if (dataLength > maxLength) {
            throw new ArithmeticException("Byte overflow in constructor.");
        }

        data = new uint[maxLength];

        for (int i = dataLength - 1, j = 0; i >= 0; i--, j++) {
            data[j] = inData[i];
        }

        while (dataLength > 1 && data[dataLength - 1] == 0) {
            dataLength--;
        }

        //Console.WriteLine("Len = " + dataLength);
    }


    //***********************************************************************
    // Overloading of the typecast operator.
    // For BigInteger bi = 10;
    //***********************************************************************

    public static implicit operator BigInteger(long value) => new(value);

    public static implicit operator BigInteger(ulong value) => new(value);

    public static implicit operator BigInteger(int value) => new(value);

    public static implicit operator BigInteger(uint value) => new((ulong) value);


    //***********************************************************************
    // Overloading of addition operator
    //***********************************************************************

    public static BigInteger operator +(BigInteger bi1, BigInteger bi2) {
        var result = new BigInteger();

        result.dataLength = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

        long carry = 0;
        for (var i = 0; i < result.dataLength; i++) {
            var sum = bi1.data[i] + (long) bi2.data[i] + carry;
            carry = sum >> 32;
            result.data[i] = (uint) (sum & 0xFFFFFFFF);
        }

        if (carry != 0 && result.dataLength < maxLength) {
            result.data[result.dataLength] = (uint) carry;
            result.dataLength++;
        }

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }


        // overflow check
        var lastPos = maxLength - 1;
        if ((bi1.data[lastPos] & 0x80000000) == (bi2.data[lastPos] & 0x80000000) &&
            (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000)) {
            throw new ArithmeticException();
        }

        return result;
    }

    //***********************************************************************
    // Overloading of subtraction operator
    //***********************************************************************

    public static BigInteger operator -(BigInteger bi1, BigInteger bi2) {
        var result = new BigInteger();

        result.dataLength = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

        long carryIn = 0;
        for (var i = 0; i < result.dataLength; i++) {
            long diff;

            diff = bi1.data[i] - (long) bi2.data[i] - carryIn;
            result.data[i] = (uint) (diff & 0xFFFFFFFF);

            if (diff < 0) {
                carryIn = 1;
            } else {
                carryIn = 0;
            }
        }

        // roll over to negative
        if (carryIn != 0) {
            for (var i = result.dataLength; i < maxLength; i++) {
                result.data[i] = 0xFFFFFFFF;
            }

            result.dataLength = maxLength;
        }

        // fixed in v1.03 to give correct datalength for a - (-b)
        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        // overflow check

        var lastPos = maxLength - 1;
        if ((bi1.data[lastPos] & 0x80000000) != (bi2.data[lastPos] & 0x80000000) &&
            (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000)) {
            throw new ArithmeticException();
        }

        return result;
    }

    //***********************************************************************
    // Overloading of multiplication operator
    //***********************************************************************

    public static BigInteger operator *(BigInteger bi1, BigInteger bi2) {
        var lastPos = maxLength - 1;
        bool bi1Neg = false, bi2Neg = false;

        // take the absolute value of the inputs
        try {
            if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
            {
                bi1Neg = true;
                bi1 = -bi1;
            }

            if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
            {
                bi2Neg = true;
                bi2 = -bi2;
            }
        } catch (Exception) {
        }

        var result = new BigInteger();

        // multiply the absolute values
        try {
            for (var i = 0; i < bi1.dataLength; i++) {
                if (bi1.data[i] == 0) {
                    continue;
                }

                ulong mcarry = 0;
                for (int j = 0, k = i; j < bi2.dataLength; j++, k++) {
                    // k = i + j
                    var val = bi1.data[i] * (ulong) bi2.data[j] + result.data[k] + mcarry;

                    result.data[k] = (uint) (val & 0xFFFFFFFF);
                    mcarry = val >> 32;
                }

                if (mcarry != 0) {
                    result.data[i + bi2.dataLength] = (uint) mcarry;
                }
            }
        } catch (Exception) {
            throw new ArithmeticException("Multiplication overflow.");
        }


        result.dataLength = bi1.dataLength + bi2.dataLength;
        if (result.dataLength > maxLength) {
            result.dataLength = maxLength;
        }

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        // overflow check (result is -ve)
        if ((result.data[lastPos] & 0x80000000) != 0) {
            if (bi1Neg != bi2Neg && result.data[lastPos] == 0x80000000) // different sign
            {
                // handle the special case where multiplication produces
                // a max negative number in 2's complement.

                if (result.dataLength == 1) {
                    return result;
                }

                var isMaxNeg = true;
                for (var i = 0; i < result.dataLength - 1 && isMaxNeg; i++) {
                    if (result.data[i] != 0) {
                        isMaxNeg = false;
                    }
                }

                if (isMaxNeg) {
                    return result;
                }
            }

            throw new ArithmeticException("Multiplication overflow.");
        }

        // if input has different signs, then result is -ve
        if (bi1Neg != bi2Neg) {
            return -result;
        }

        return result;
    }


    //***********************************************************************
    // Overloading of unary << operators
    //***********************************************************************

    public static BigInteger operator <<(BigInteger bi1, int shiftVal) {
        var result = new BigInteger(bi1);
        result.dataLength = shiftLeft(result.data, shiftVal);

        return result;
    }


    // least significant bits at lower part of buffer

    static int shiftLeft(uint[] buffer, int shiftVal) {
        var shiftAmount = 32;
        var bufLen = buffer.Length;

        while (bufLen > 1 && buffer[bufLen - 1] == 0) {
            bufLen--;
        }

        for (var count = shiftVal; count > 0;) {
            if (count < shiftAmount) {
                shiftAmount = count;
            }

            //Console.WriteLine("shiftAmount = {0}", shiftAmount);

            ulong carry = 0;
            for (var i = 0; i < bufLen; i++) {
                var val = (ulong) buffer[i] << shiftAmount;
                val |= carry;

                buffer[i] = (uint) (val & 0xFFFFFFFF);
                carry = val >> 32;
            }

            if (carry != 0) {
                if (bufLen + 1 <= buffer.Length) {
                    buffer[bufLen] = (uint) carry;
                    bufLen++;
                }
            }

            count -= shiftAmount;
        }

        return bufLen;
    }


    //***********************************************************************
    // Overloading of unary >> operators
    //***********************************************************************

    public static BigInteger operator >> (BigInteger bi1, int shiftVal) {
        var result = new BigInteger(bi1);
        result.dataLength = shiftRight(result.data, shiftVal);


        if ((bi1.data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            for (var i = maxLength - 1; i >= result.dataLength; i--) {
                result.data[i] = 0xFFFFFFFF;
            }

            var mask = 0x80000000;
            for (var i = 0; i < 32; i++) {
                if ((result.data[result.dataLength - 1] & mask) != 0) {
                    break;
                }

                result.data[result.dataLength - 1] |= mask;
                mask >>= 1;
            }

            result.dataLength = maxLength;
        }

        return result;
    }


    static int shiftRight(uint[] buffer, int shiftVal) {
        var shiftAmount = 32;
        var invShift = 0;
        var bufLen = buffer.Length;

        while (bufLen > 1 && buffer[bufLen - 1] == 0) {
            bufLen--;
        }

        //Console.WriteLine("bufLen = " + bufLen + " buffer.Length = " + buffer.Length);

        for (var count = shiftVal; count > 0;) {
            if (count < shiftAmount) {
                shiftAmount = count;
                invShift = 32 - shiftAmount;
            }

            //Console.WriteLine("shiftAmount = {0}", shiftAmount);

            ulong carry = 0;
            for (var i = bufLen - 1; i >= 0; i--) {
                var val = (ulong) buffer[i] >> shiftAmount;
                val |= carry;

                carry = (ulong) buffer[i] << invShift;
                buffer[i] = (uint) val;
            }

            count -= shiftAmount;
        }

        while (bufLen > 1 && buffer[bufLen - 1] == 0) {
            bufLen--;
        }

        return bufLen;
    }


    //***********************************************************************
    // Overloading of the NEGATE operator (2's complement)
    //***********************************************************************

    public static BigInteger operator -(BigInteger bi1) {
        // handle neg of zero separately since it'll cause an overflow
        // if we proceed.

        if (bi1.dataLength == 1 && bi1.data[0] == 0) {
            return new BigInteger();
        }

        var result = new BigInteger(bi1);

        // 1's complement
        for (var i = 0; i < maxLength; i++) {
            result.data[i] = ~bi1.data[i];
        }

        // add one to result of 1's complement
        long val, carry = 1;
        var index = 0;

        while (carry != 0 && index < maxLength) {
            val = result.data[index];
            val++;

            result.data[index] = (uint) (val & 0xFFFFFFFF);
            carry = val >> 32;

            index++;
        }

        if ((bi1.data[maxLength - 1] & 0x80000000) == (result.data[maxLength - 1] & 0x80000000)) {
            throw new ArithmeticException("Overflow in negation.\n");
        }

        result.dataLength = maxLength;

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        return result;
    }


    //***********************************************************************
    // Overloading of equality operator
    //***********************************************************************

    public static bool operator ==(BigInteger bi1, BigInteger bi2) => bi1.Equals(bi2);


    public static bool operator !=(BigInteger bi1, BigInteger bi2) => !bi1.Equals(bi2);


    public override bool Equals(object o) {
        var bi = (BigInteger) o;

        if (dataLength != bi.dataLength) {
            return false;
        }

        for (var i = 0; i < dataLength; i++) {
            if (data[i] != bi.data[i]) {
                return false;
            }
        }

        return true;
    }


    public override int GetHashCode() => ToString().GetHashCode();


    //***********************************************************************
    // Overloading of inequality operator
    //***********************************************************************

    public static bool operator >(BigInteger bi1, BigInteger bi2) {
        var pos = maxLength - 1;

        // bi1 is negative, bi2 is positive
        if ((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0) {
            return false;
        }

        // bi1 is positive, bi2 is negative
        if ((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0) {
            return true;
        }

        // same sign
        var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;
        for (pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--) {
            ;
        }

        if (pos >= 0) {
            if (bi1.data[pos] > bi2.data[pos]) {
                return true;
            }

            return false;
        }

        return false;
    }


    public static bool operator <(BigInteger bi1, BigInteger bi2) {
        var pos = maxLength - 1;

        // bi1 is negative, bi2 is positive
        if ((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0) {
            return true;
        }

        // bi1 is positive, bi2 is negative
        if ((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0) {
            return false;
        }

        // same sign
        var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;
        for (pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--) {
            ;
        }

        if (pos >= 0) {
            if (bi1.data[pos] < bi2.data[pos]) {
                return true;
            }

            return false;
        }

        return false;
    }


    public static bool operator >=(BigInteger bi1, BigInteger bi2) => bi1 == bi2 || bi1 > bi2;


    public static bool operator <=(BigInteger bi1, BigInteger bi2) => bi1 == bi2 || bi1 < bi2;


    //***********************************************************************
    // Private function that supports the division of two numbers with
    // a divisor that has more than 1 digit.
    //
    // Algorithm taken from [1]
    //***********************************************************************

    static void multiByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient,
        BigInteger outRemainder) {
        var result = new uint[maxLength];

        var remainderLen = bi1.dataLength + 1;
        var remainder = new uint[remainderLen];

        var mask = 0x80000000;
        var val = bi2.data[bi2.dataLength - 1];
        int shift = 0, resultPos = 0;

        while (mask != 0 && (val & mask) == 0) {
            shift++;
            mask >>= 1;
        }

        //Console.WriteLine("shift = {0}", shift);
        //Console.WriteLine("Before bi1 Len = {0}, bi2 Len = {1}", bi1.dataLength, bi2.dataLength);

        for (var i = 0; i < bi1.dataLength; i++) {
            remainder[i] = bi1.data[i];
        }

        shiftLeft(remainder, shift);
        bi2 <<= shift;

        /*
        Console.WriteLine("bi1 Len = {0}, bi2 Len = {1}", bi1.dataLength, bi2.dataLength);
        Console.WriteLine("dividend = " + bi1 + "\ndivisor = " + bi2);
        for(int q = remainderLen - 1; q >= 0; q--)
                Console.Write("{0:x2}", remainder[q]);
        Console.WriteLine();
        */

        var j = remainderLen - bi2.dataLength;
        var pos = remainderLen - 1;

        ulong firstDivisorByte = bi2.data[bi2.dataLength - 1];
        ulong secondDivisorByte = bi2.data[bi2.dataLength - 2];

        var divisorLen = bi2.dataLength + 1;
        var dividendPart = new uint[divisorLen];

        while (j > 0) {
            var dividend = ((ulong) remainder[pos] << 32) + remainder[pos - 1];
            //Console.WriteLine("dividend = {0}", dividend);

            var q_hat = dividend / firstDivisorByte;
            var r_hat = dividend % firstDivisorByte;

            //Console.WriteLine("q_hat = {0:X}, r_hat = {1:X}", q_hat, r_hat);

            var done = false;
            while (!done) {
                done = true;

                if (q_hat == 0x100000000 ||
                    q_hat * secondDivisorByte > (r_hat << 32) + remainder[pos - 2]) {
                    q_hat--;
                    r_hat += firstDivisorByte;

                    if (r_hat < 0x100000000) {
                        done = false;
                    }
                }
            }

            for (var h = 0; h < divisorLen; h++) {
                dividendPart[h] = remainder[pos - h];
            }

            var kk = new BigInteger(dividendPart);
            var ss = bi2 * (long) q_hat;

            //Console.WriteLine("ss before = " + ss);
            while (ss > kk) {
                q_hat--;
                ss -= bi2;
                //Console.WriteLine(ss);
            }

            var yy = kk - ss;

            //Console.WriteLine("ss = " + ss);
            //Console.WriteLine("kk = " + kk);
            //Console.WriteLine("yy = " + yy);

            for (var h = 0; h < divisorLen; h++) {
                remainder[pos - h] = yy.data[bi2.dataLength - h];
            }

            /*
            Console.WriteLine("dividend = ");
            for(int q = remainderLen - 1; q >= 0; q--)
                    Console.Write("{0:x2}", remainder[q]);
            Console.WriteLine("\n************ q_hat = {0:X}\n", q_hat);
            */

            result[resultPos++] = (uint) q_hat;

            pos--;
            j--;
        }

        outQuotient.dataLength = resultPos;
        var y = 0;
        for (var x = outQuotient.dataLength - 1; x >= 0; x--, y++) {
            outQuotient.data[y] = result[x];
        }

        for (; y < maxLength; y++) {
            outQuotient.data[y] = 0;
        }

        while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0) {
            outQuotient.dataLength--;
        }

        if (outQuotient.dataLength == 0) {
            outQuotient.dataLength = 1;
        }

        outRemainder.dataLength = shiftRight(remainder, shift);

        for (y = 0; y < outRemainder.dataLength; y++) {
            outRemainder.data[y] = remainder[y];
        }

        for (; y < maxLength; y++) {
            outRemainder.data[y] = 0;
        }
    }


    //***********************************************************************
    // Private function that supports the division of two numbers with
    // a divisor that has only 1 digit.
    //***********************************************************************

    static void singleByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient,
        BigInteger outRemainder) {
        var result = new uint[maxLength];
        var resultPos = 0;

        // copy dividend to reminder
        for (var i = 0; i < maxLength; i++) {
            outRemainder.data[i] = bi1.data[i];
        }

        outRemainder.dataLength = bi1.dataLength;

        while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0) {
            outRemainder.dataLength--;
        }

        var divisor = (ulong) bi2.data[0];
        var pos = outRemainder.dataLength - 1;
        var dividend = (ulong) outRemainder.data[pos];

        //Console.WriteLine("divisor = " + divisor + " dividend = " + dividend);
        //Console.WriteLine("divisor = " + bi2 + "\ndividend = " + bi1);

        if (dividend >= divisor) {
            var quotient = dividend / divisor;
            result[resultPos++] = (uint) quotient;

            outRemainder.data[pos] = (uint) (dividend % divisor);
        }

        pos--;

        while (pos >= 0) {
            //Console.WriteLine(pos);

            dividend = ((ulong) outRemainder.data[pos + 1] << 32) + outRemainder.data[pos];
            var quotient = dividend / divisor;
            result[resultPos++] = (uint) quotient;

            outRemainder.data[pos + 1] = 0;
            outRemainder.data[pos--] = (uint) (dividend % divisor);
            //Console.WriteLine(">>>> " + bi1);
        }

        outQuotient.dataLength = resultPos;
        var j = 0;
        for (var i = outQuotient.dataLength - 1; i >= 0; i--, j++) {
            outQuotient.data[j] = result[i];
        }

        for (; j < maxLength; j++) {
            outQuotient.data[j] = 0;
        }

        while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0) {
            outQuotient.dataLength--;
        }

        if (outQuotient.dataLength == 0) {
            outQuotient.dataLength = 1;
        }

        while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0) {
            outRemainder.dataLength--;
        }
    }


    //***********************************************************************
    // Overloading of division operator
    //***********************************************************************

    public static BigInteger operator /(BigInteger bi1, BigInteger bi2) {
        var quotient = new BigInteger();
        var remainder = new BigInteger();

        var lastPos = maxLength - 1;
        bool divisorNeg = false, dividendNeg = false;

        if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
        {
            bi1 = -bi1;
            dividendNeg = true;
        }

        if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
        {
            bi2 = -bi2;
            divisorNeg = true;
        }

        if (bi1 < bi2) {
            return quotient;
        }

        if (bi2.dataLength == 1) {
            singleByteDivide(bi1, bi2, quotient, remainder);
        } else {
            multiByteDivide(bi1, bi2, quotient, remainder);
        }

        if (dividendNeg != divisorNeg) {
            return -quotient;
        }

        return quotient;
    }


    //***********************************************************************
    // Overloading of modulus operator
    //***********************************************************************

    public static BigInteger operator %(BigInteger bi1, BigInteger bi2) {
        var quotient = new BigInteger();
        var remainder = new BigInteger(bi1);

        var lastPos = maxLength - 1;
        var dividendNeg = false;

        if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
        {
            bi1 = -bi1;
            dividendNeg = true;
        }

        if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
        {
            bi2 = -bi2;
        }

        if (bi1 < bi2) {
            return remainder;
        }

        if (bi2.dataLength == 1) {
            singleByteDivide(bi1, bi2, quotient, remainder);
        } else {
            multiByteDivide(bi1, bi2, quotient, remainder);
        }

        if (dividendNeg) {
            return -remainder;
        }

        return remainder;
    }


    //***********************************************************************
    // Overloading of bitwise AND operator
    //***********************************************************************

    public static BigInteger operator &(BigInteger bi1, BigInteger bi2) {
        var result = new BigInteger();

        var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

        for (var i = 0; i < len; i++) {
            var sum = bi1.data[i] & bi2.data[i];
            result.data[i] = sum;
        }

        result.dataLength = maxLength;

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        return result;
    }


    //***********************************************************************
    // Overloading of bitwise OR operator
    //***********************************************************************

    public static BigInteger operator |(BigInteger bi1, BigInteger bi2) {
        var result = new BigInteger();

        var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

        for (var i = 0; i < len; i++) {
            var sum = bi1.data[i] | bi2.data[i];
            result.data[i] = sum;
        }

        result.dataLength = maxLength;

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        return result;
    }


    //***********************************************************************
    // Overloading of bitwise XOR operator
    //***********************************************************************

    public static BigInteger operator ^(BigInteger bi1, BigInteger bi2) {
        var result = new BigInteger();

        var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

        for (var i = 0; i < len; i++) {
            var sum = bi1.data[i] ^ bi2.data[i];
            result.data[i] = sum;
        }

        result.dataLength = maxLength;

        while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) {
            result.dataLength--;
        }

        return result;
    }


    //***********************************************************************
    // Returns max(this, bi)
    //***********************************************************************

    public BigInteger max(BigInteger bi) {
        if (this > bi) {
            return new BigInteger(this);
        }

        return new BigInteger(bi);
    }


    //***********************************************************************
    // Returns min(this, bi)
    //***********************************************************************

    public BigInteger min(BigInteger bi) {
        if (this < bi) {
            return new BigInteger(this);
        }

        return new BigInteger(bi);
    }


    //***********************************************************************
    // Returns the absolute value
    //***********************************************************************

    public BigInteger abs() {
        if ((data[maxLength - 1] & 0x80000000) != 0) {
            return -this;
        }

        return new BigInteger(this);
    }


    //***********************************************************************
    // Returns a string representing the BigInteger in base 10.
    //***********************************************************************

    public override string ToString() => ToString(10);


    //***********************************************************************
    // Returns a string representing the BigInteger in sign-and-magnitude
    // format in the specified radix.
    //
    // Example
    // -------
    // If the value of BigInteger is -255 in base 10, then
    // ToString(16) returns "-FF"
    //
    //***********************************************************************

    public string ToString(int radix) {
        if (radix < 2 || radix > 36) {
            throw new ArgumentException("Radix must be >= 2 and <= 36");
        }

        var charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = "";

        var a = this;

        var negative = false;
        if ((a.data[maxLength - 1] & 0x80000000) != 0) {
            negative = true;
            try {
                a = -a;
            } catch (Exception) {
            }
        }

        var quotient = new BigInteger();
        var remainder = new BigInteger();
        var biRadix = new BigInteger(radix);

        if (a.dataLength == 1 && a.data[0] == 0) {
            result = "0";
        } else {
            while (a.dataLength > 1 || a.dataLength == 1 && a.data[0] != 0) {
                singleByteDivide(a, biRadix, quotient, remainder);

                if (remainder.data[0] < 10) {
                    result = remainder.data[0] + result;
                } else {
                    result = charSet[(int) remainder.data[0] - 10] + result;
                }

                a = quotient;
            }

            if (negative) {
                result = "-" + result;
            }
        }

        return result;
    }


    //***********************************************************************
    // Returns a hex string showing the contains of the BigInteger
    //
    // Examples
    // -------
    // 1) If the value of BigInteger is 255 in base 10, then
    //    ToHexString() returns "FF"
    //
    // 2) If the value of BigInteger is -255 in base 10, then
    //    ToHexString() returns ".....FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF01",
    //    which is the 2's complement representation of -255.
    //
    //***********************************************************************

    public string ToHexString() {
        var result = data[dataLength - 1].ToString("X");

        for (var i = dataLength - 2; i >= 0; i--) {
            result += data[i].ToString("X8");
        }

        return result;
    }


    //***********************************************************************
    // Modulo Exponentiation
    //***********************************************************************

    public BigInteger modPow(BigInteger exp, BigInteger n) {
        if ((exp.data[maxLength - 1] & 0x80000000) != 0) {
            throw new ArithmeticException("Positive exponents only.");
        }

        BigInteger resultNum = 1;
        BigInteger tempNum;
        var thisNegative = false;

        if ((data[maxLength - 1] & 0x80000000) != 0) // negative this
        {
            tempNum = -this % n;
            thisNegative = true;
        } else {
            tempNum = this % n; // ensures (tempNum * tempNum) < b^(2k)
        }

        if ((n.data[maxLength - 1] & 0x80000000) != 0) // negative n
        {
            n = -n;
        }

        // calculate constant = b^(2k) / m
        var constant = new BigInteger();

        var i = n.dataLength << 1;
        constant.data[i] = 0x00000001;
        constant.dataLength = i + 1;

        constant /= n;
        var totalBits = exp.bitCount();
        var count = 0;

        // perform squaring and multiply exponentiation
        for (var pos = 0; pos < exp.dataLength; pos++) {
            uint mask = 0x01;
            //Console.WriteLine("pos = " + pos);

            for (var index = 0; index < 32; index++) {
                if ((exp.data[pos] & mask) != 0) {
                    resultNum = BarrettReduction(resultNum * tempNum, n, constant);
                }

                mask <<= 1;

                tempNum = BarrettReduction(tempNum * tempNum, n, constant);


                if (tempNum.dataLength == 1 && tempNum.data[0] == 1) {
                    if (thisNegative && (exp.data[0] & 0x1) != 0) //odd exp
                    {
                        return -resultNum;
                    }

                    return resultNum;
                }

                count++;
                if (count == totalBits) {
                    break;
                }
            }
        }

        if (thisNegative && (exp.data[0] & 0x1) != 0) //odd exp
        {
            return -resultNum;
        }

        return resultNum;
    }


    //***********************************************************************
    // Fast calculation of modular reduction using Barrett's reduction.
    // Requires x < b^(2k), where b is the base.  In this case, base is
    // 2^32 (uint).
    //
    // Reference [4]
    //***********************************************************************

    BigInteger BarrettReduction(BigInteger x, BigInteger n, BigInteger constant) {
        int k = n.dataLength, kPlusOne = k + 1, kMinusOne = k - 1;

        var q1 = new BigInteger();

        // q1 = x / b^(k-1)
        for (int i = kMinusOne, j = 0; i < x.dataLength; i++, j++) {
            q1.data[j] = x.data[i];
        }

        q1.dataLength = x.dataLength - kMinusOne;
        if (q1.dataLength <= 0) {
            q1.dataLength = 1;
        }


        var q2 = q1 * constant;
        var q3 = new BigInteger();

        // q3 = q2 / b^(k+1)
        for (int i = kPlusOne, j = 0; i < q2.dataLength; i++, j++) {
            q3.data[j] = q2.data[i];
        }

        q3.dataLength = q2.dataLength - kPlusOne;
        if (q3.dataLength <= 0) {
            q3.dataLength = 1;
        }


        // r1 = x mod b^(k+1)
        // i.e. keep the lowest (k+1) words
        var r1 = new BigInteger();
        var lengthToCopy = x.dataLength > kPlusOne ? kPlusOne : x.dataLength;
        for (var i = 0; i < lengthToCopy; i++) {
            r1.data[i] = x.data[i];
        }

        r1.dataLength = lengthToCopy;


        // r2 = (q3 * n) mod b^(k+1)
        // partial multiplication of q3 and n

        var r2 = new BigInteger();
        for (var i = 0; i < q3.dataLength; i++) {
            if (q3.data[i] == 0) {
                continue;
            }

            ulong mcarry = 0;
            var t = i;
            for (var j = 0; j < n.dataLength && t < kPlusOne; j++, t++) {
                // t = i + j
                var val = q3.data[i] * (ulong) n.data[j] + r2.data[t] + mcarry;

                r2.data[t] = (uint) (val & 0xFFFFFFFF);
                mcarry = val >> 32;
            }

            if (t < kPlusOne) {
                r2.data[t] = (uint) mcarry;
            }
        }

        r2.dataLength = kPlusOne;
        while (r2.dataLength > 1 && r2.data[r2.dataLength - 1] == 0) {
            r2.dataLength--;
        }

        r1 -= r2;
        if ((r1.data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            var val = new BigInteger();
            val.data[kPlusOne] = 0x00000001;
            val.dataLength = kPlusOne + 1;
            r1 += val;
        }

        while (r1 >= n) {
            r1 -= n;
        }

        return r1;
    }


    //***********************************************************************
    // Returns gcd(this, bi)
    //***********************************************************************

    public BigInteger gcd(BigInteger bi) {
        BigInteger x;
        BigInteger y;

        if ((data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            x = -this;
        } else {
            x = this;
        }

        if ((bi.data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            y = -bi;
        } else {
            y = bi;
        }

        var g = y;

        while (x.dataLength > 1 || x.dataLength == 1 && x.data[0] != 0) {
            g = x;
            x = y % x;
            y = g;
        }

        return g;
    }


    //***********************************************************************
    // Populates "this" with the specified amount of random bits
    //***********************************************************************

    public void genRandomBits(int bits, Random rand) {
        var dwords = bits >> 5;
        var remBits = bits & 0x1F;

        if (remBits != 0) {
            dwords++;
        }

        if (dwords > maxLength) {
            throw new ArithmeticException("Number of required bits > maxLength.");
        }

        for (var i = 0; i < dwords; i++) {
            data[i] = (uint) (rand.NextDouble() * 0x100000000);
        }

        for (var i = dwords; i < maxLength; i++) {
            data[i] = 0;
        }

        if (remBits != 0) {
            var mask = (uint) (0x01 << (remBits - 1));
            data[dwords - 1] |= mask;

            mask = 0xFFFFFFFF >> (32 - remBits);
            data[dwords - 1] &= mask;
        } else {
            data[dwords - 1] |= 0x80000000;
        }

        dataLength = dwords;

        if (dataLength == 0) {
            dataLength = 1;
        }
    }


    //***********************************************************************
    // Returns the position of the most significant bit in the BigInteger.
    //
    // Eg.  The result is 0, if the value of BigInteger is 0...0000 0000
    //      The result is 1, if the value of BigInteger is 0...0000 0001
    //      The result is 2, if the value of BigInteger is 0...0000 0010
    //      The result is 2, if the value of BigInteger is 0...0000 0011
    //
    //***********************************************************************

    public int bitCount() {
        while (dataLength > 1 && data[dataLength - 1] == 0) {
            dataLength--;
        }

        var value = data[dataLength - 1];
        var mask = 0x80000000;
        var bits = 32;

        while (bits > 0 && (value & mask) == 0) {
            bits--;
            mask >>= 1;
        }

        bits += (dataLength - 1) << 5;

        return bits;
    }

    //***********************************************************************
    // Probabilistic prime test based on Rabin-Miller's
    //
    // for any p > 0 with p - 1 = 2^s * t
    //
    // p is probably prime (strong pseudoprime) if for any a < p,
    // 1) a^t mod p = 1 or
    // 2) a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
    //
    // Otherwise, p is composite.
    //
    // Returns
    // -------
    // True if "this" is a strong pseudoprime to randomly chosen
    // bases.  The number of chosen bases is given by the "confidence"
    // parameter.
    //
    // False if "this" is definitely NOT prime.
    //
    //***********************************************************************

    public bool RabinMillerTest(int confidence) {
        BigInteger thisVal;
        if ((data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            thisVal = -this;
        } else {
            thisVal = this;
        }

        if (thisVal.dataLength == 1) {
            // test small numbers
            if (thisVal.data[0] == 0 || thisVal.data[0] == 1) {
                return false;
            }

            if (thisVal.data[0] == 2 || thisVal.data[0] == 3) {
                return true;
            }
        }

        if ((thisVal.data[0] & 0x1) == 0) // even numbers
        {
            return false;
        }


        // calculate values of s and t
        var p_sub1 = thisVal - new BigInteger(1);
        var s = 0;

        for (var index = 0; index < p_sub1.dataLength; index++) {
            uint mask = 0x01;

            for (var i = 0; i < 32; i++) {
                if ((p_sub1.data[index] & mask) != 0) {
                    index = p_sub1.dataLength; // to break the outer loop
                    break;
                }

                mask <<= 1;
                s++;
            }
        }

        var t = p_sub1 >> s;

        var bits = thisVal.bitCount();
        var a = new BigInteger();
        var rand = new Random();

        for (var round = 0; round < confidence; round++) {
            var done = false;

            while (!done) // generate a < n
            {
                var testBits = 0;

                // make sure "a" has at least 2 bits
                while (testBits < 2) {
                    testBits = (int) (rand.NextDouble() * bits);
                }

                a.genRandomBits(testBits, rand);

                var byteLen = a.dataLength;

                // make sure "a" is not 0
                if (byteLen > 1 || byteLen == 1 && a.data[0] != 1) {
                    done = true;
                }
            }

            // check whether a factor exists (fix for version 1.03)
            var gcdTest = a.gcd(thisVal);
            if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1) {
                return false;
            }

            var b = a.modPow(t, thisVal);

            /*
            Console.WriteLine("a = " + a.ToString(10));
            Console.WriteLine("b = " + b.ToString(10));
            Console.WriteLine("t = " + t.ToString(10));
            Console.WriteLine("s = " + s);
            */

            var result = false;

            if (b.dataLength == 1 && b.data[0] == 1) // a^t mod p = 1
            {
                result = true;
            }

            for (var j = 0; result == false && j < s; j++) {
                if (b == p_sub1) // a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
                {
                    result = true;
                    break;
                }

                b = b * b % thisVal;
            }

            if (result == false) {
                return false;
            }
        }

        return true;
    }

    //***********************************************************************
    // Determines whether a number is probably prime, using the Rabin-Miller's
    // test.  Before applying the test, the number is tested for divisibility
    // by primes < 2000
    //
    // Returns true if number is probably prime.
    //***********************************************************************

    public bool isProbablePrime(int confidence) {
        BigInteger thisVal;
        if ((data[maxLength - 1] & 0x80000000) != 0) // negative
        {
            thisVal = -this;
        } else {
            thisVal = this;
        }


        // test for divisibility by primes < 2000
        for (var p = 0; p < primesBelow2000.Length; p++) {
            BigInteger divisor = primesBelow2000[p];

            if (divisor >= thisVal) {
                break;
            }

            var resultNum = thisVal % divisor;
            if (resultNum.IntValue() == 0) {
                return false;
            }
        }

        if (thisVal.RabinMillerTest(confidence)) {
            return true;
        }

        return false;
    }

    //***********************************************************************
    // Returns the lowest 4 bytes of the BigInteger as an int.
    //***********************************************************************

    public int IntValue() => (int) data[0];


    //***********************************************************************
    // Computes the Jacobi Symbol for a and b.
    // Algorithm adapted from [3] and [4] with some optimizations
    //***********************************************************************

    public static int Jacobi(BigInteger a, BigInteger b) {
        // Jacobi defined only for odd integers
        if ((b.data[0] & 0x1) == 0) {
            throw new ArgumentException("Jacobi defined only for odd integers.");
        }

        if (a >= b) {
            a %= b;
        }

        if (a.dataLength == 1 && a.data[0] == 0) {
            return 0; // a == 0
        }

        if (a.dataLength == 1 && a.data[0] == 1) {
            return 1; // a == 1
        }

        if (a < 0) {
            if (((b - 1).data[0] & 0x2) == 0) //if( (((b-1) >> 1).data[0] & 0x1) == 0)
            {
                return Jacobi(-a, b);
            }

            return -Jacobi(-a, b);
        }

        var e = 0;
        for (var index = 0; index < a.dataLength; index++) {
            uint mask = 0x01;

            for (var i = 0; i < 32; i++) {
                if ((a.data[index] & mask) != 0) {
                    index = a.dataLength; // to break the outer loop
                    break;
                }

                mask <<= 1;
                e++;
            }
        }

        var a1 = a >> e;

        var s = 1;
        if ((e & 0x1) != 0 && ((b.data[0] & 0x7) == 3 || (b.data[0] & 0x7) == 5)) {
            s = -1;
        }

        if ((b.data[0] & 0x3) == 3 && (a1.data[0] & 0x3) == 3) {
            s = -s;
        }

        if (a1.dataLength == 1 && a1.data[0] == 1) {
            return s;
        }

        return s * Jacobi(b % a1, a1);
    }


    //***********************************************************************
    // Generates a positive BigInteger that is probably prime.
    //***********************************************************************

    public static BigInteger genPseudoPrime(int bits, int confidence, Random rand) {
        var result = new BigInteger();
        var done = false;

        while (!done) {
            result.genRandomBits(bits, rand);
            result.data[0] |= 0x01; // make it odd

            // prime test
            done = result.isProbablePrime(confidence);
        }

        return result;
    }


    //***********************************************************************
    // Generates a random number with the specified number of bits such
    // that gcd(number, this) = 1
    //***********************************************************************

    public BigInteger genCoPrime(int bits, Random rand) {
        var done = false;
        var result = new BigInteger();

        while (!done) {
            result.genRandomBits(bits, rand);
            //Console.WriteLine(result.ToString(16));

            // gcd test
            var g = result.gcd(this);
            if (g.dataLength == 1 && g.data[0] == 1) {
                done = true;
            }
        }

        return result;
    }

    //***********************************************************************
    // Returns the value of the BigInteger as a byte array.  The lowest
    // index contains the MSB.
    //***********************************************************************

    public byte[] getBytes() {
        var numBits = bitCount();

        var numBytes = numBits >> 3;
        if ((numBits & 0x7) != 0) {
            numBytes++;
        }

        var result = new byte[numBytes];

        //Console.WriteLine(result.Length);

        var pos = 0;
        uint tempVal, val = data[dataLength - 1];

        if ((tempVal = (val >> 24) & 0xFF) != 0) {
            result[pos++] = (byte) tempVal;
        }

        if ((tempVal = (val >> 16) & 0xFF) != 0) {
            result[pos++] = (byte) tempVal;
        }

        if ((tempVal = (val >> 8) & 0xFF) != 0) {
            result[pos++] = (byte) tempVal;
        }

        if ((tempVal = val & 0xFF) != 0) {
            result[pos++] = (byte) tempVal;
        }

        for (var i = dataLength - 2; i >= 0; i--, pos += 4) {
            val = data[i];
            result[pos + 3] = (byte) (val & 0xFF);
            val >>= 8;
            result[pos + 2] = (byte) (val & 0xFF);
            val >>= 8;
            result[pos + 1] = (byte) (val & 0xFF);
            val >>= 8;
            result[pos] = (byte) (val & 0xFF);
        }

        return result;
    }

    //***********************************************************************
    // Returns a value that is equivalent to the integer square root
    // of the BigInteger.
    //
    // The integer square root of "this" is defined as the largest integer n
    // such that (n * n) <= this
    //
    //***********************************************************************

    public BigInteger sqrt() {
        var numBits = (uint) bitCount();

        if ((numBits & 0x1) != 0) // odd number of bits
        {
            numBits = (numBits >> 1) + 1;
        } else {
            numBits >>= 1;
        }

        var bytePos = numBits >> 5;
        var bitPos = (byte) (numBits & 0x1F);

        uint mask;

        var result = new BigInteger();
        if (bitPos == 0) {
            mask = 0x80000000;
        } else {
            mask = (uint) 1 << bitPos;
            bytePos++;
        }

        result.dataLength = (int) bytePos;

        for (var i = (int) bytePos - 1; i >= 0; i--) {
            while (mask != 0) {
                // guess
                result.data[i] ^= mask;

                // undo the guess if its square is larger than this
                if (result * result > this) {
                    result.data[i] ^= mask;
                }

                mask >>= 1;
            }

            mask = 0x80000000;
        }

        return result;
    }


    //***********************************************************************
    // Performs the calculation of the kth term in the Lucas Sequence.
    // For details of the algorithm, see reference [9].
    //
    // k must be odd.  i.e LSB == 1
    //***********************************************************************

    static BigInteger[] LucasSequenceHelper(BigInteger P, BigInteger Q, BigInteger k, BigInteger n,
        BigInteger constant, int s) {
        var result = new BigInteger[3];

        if ((k.data[0] & 0x00000001) == 0) {
            throw new ArgumentException("Argument k must be odd.");
        }

        var numbits = k.bitCount();
        var mask = (uint) 0x1 << ((numbits & 0x1F) - 1);

        // v = v0, v1 = v1, u1 = u1, Q_k = Q^0

        BigInteger v = 2 % n, Q_k = 1 % n, v1 = P % n, u1 = Q_k;
        var flag = true;

        for (var i = k.dataLength - 1; i >= 0; i--) // iterate on the binary expansion of k
        {
            //Console.WriteLine("round");
            while (mask != 0) {
                if (i == 0 && mask == 0x00000001) // last bit
                {
                    break;
                }

                if ((k.data[i] & mask) != 0) // bit is set
                {
                    // index doubling with addition

                    u1 = u1 * v1 % n;

                    v = (v * v1 - P * Q_k) % n;
                    v1 = n.BarrettReduction(v1 * v1, n, constant);
                    v1 = (v1 - ((Q_k * Q) << 1)) % n;

                    if (flag) {
                        flag = false;
                    } else {
                        Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
                    }

                    Q_k = Q_k * Q % n;
                } else {
                    // index doubling
                    u1 = (u1 * v - Q_k) % n;

                    v1 = (v * v1 - P * Q_k) % n;
                    v = n.BarrettReduction(v * v, n, constant);
                    v = (v - (Q_k << 1)) % n;

                    if (flag) {
                        Q_k = Q % n;
                        flag = false;
                    } else {
                        Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
                    }
                }

                mask >>= 1;
            }

            mask = 0x80000000;
        }

        // at this point u1 = u(n+1) and v = v(n)
        // since the last bit always 1, we need to transform u1 to u(2n+1) and v to v(2n+1)

        u1 = (u1 * v - Q_k) % n;
        v = (v * v1 - P * Q_k) % n;
        if (flag) {
            flag = false;
        } else {
            Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
        }

        Q_k = Q_k * Q % n;


        for (var i = 0; i < s; i++) {
            // index doubling
            u1 = u1 * v % n;
            v = (v * v - (Q_k << 1)) % n;

            if (flag) {
                Q_k = Q % n;
                flag = false;
            } else {
                Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
            }
        }

        result[0] = u1;
        result[1] = v;
        result[2] = Q_k;

        return result;
    }
}
