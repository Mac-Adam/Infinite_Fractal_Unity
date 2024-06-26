using System;
using UnityEngine;
namespace FixedPointNumberSystem
{
    public static class GPUCode { 
        public struct GPUPrecision{
            public int precision;
            public string name;
            public GPUPrecision(int precision,string name)
            {
                this.precision = precision;
                this.name = name;
            } 
        }
        //Some shaders are multicompiled, to chose the right wersion proper keywords should be enabled
        //Use this method to make sure all the keywords are reset before turning proper ones on
        public static void ResetAllKeywords()
        {
            foreach(GPUPrecision pre in precisions)
            {
                Shader.DisableKeyword(pre.name);
            }
            Shader.DisableKeyword("FLOAT");
            Shader.DisableKeyword("DOUBLE");
            Shader.DisableKeyword("INFINITE");
            Shader.DisableKeyword("ITER");
            Shader.DisableKeyword("IN");
            Shader.DisableKeyword("OUT");

        }
        public static void SetKeywords(string[] keywords)
        {
            ResetAllKeywords();
            foreach (string word in keywords)
            {
                Shader.EnableKeyword(word);
            }
        }

        public static GPUPrecision[] precisions = {
            new(4,"Q_VERY_LOW"),
            new(6,"Q_LOW"),
            new(8,"Q_MED"),
            new(10,"Q_HIGH"),
            new(12,"Q_VERY_HIGH"),
            new(14,"Q_ULTRA"),
            new(16,"Q_ULTRA_HIGH"),
            new(18,"Q_SUPER"),
            new(20,"Q_MEGA"),
            new(24,"Q_SUPER_MEGA"),
            };

    }
    public struct FixedPointNumber
    {
     
        public static int digitBase = 46300;
        //for simplicity it is assumed that both numers have the same precision
        public int precision;
        public int[] digits;

        public FixedPointNumber(int pre)
        {
            precision = pre;
            digits = new int[pre];
            for (int i = 0; i < precision; i++)
            {
                digits[i] = 0;
            }
        }

        public FixedPointNumber(FixedPointNumber other)
        {
            precision=other.precision;
            digits = new int[precision];
            for (int i = 0; i < precision; i++)
            {
                digits[i] = other.digits[i];
            }
        }
        public void SetDouble(double num)
        {
            bool negate = false;
            if (num < 0)
            {
                negate = true;
                num = -num;
            }
            double temp = num;
            for (int i = 0; i < precision; i++)
            {
                digits[i] = (int)temp;
                temp = (temp - digits[i]) * digitBase;
            }
            if (negate)
            {
                Negate();
            }
        }
        public override string ToString()
        {
            string res = "";
            res += digits[0];
            res += ".";
            for (int i = 1; i < precision; i++)
            {
                res += digits[i].ToString("00000$");
            }
            return res;
        }
        public void IncresePrecision(int newPrecision)
        {
            int[] temp = digits;
            digits = new int[newPrecision];
            for (int i = 0; i < newPrecision; i++)
            {
                digits[i] = i < precision ? temp[i] : 0;
            }
            precision = newPrecision;
        }
        public bool IsPositive()
        {
            for( int i = 0; i < precision; i++)
            {
                if (digits[i] != 0)
                {
                    return digits[i] > 0;
                }
            }
            return true;
        }
        public void BorrowUp()
        {
            digits[0]--;
            for(int i = 1; i < precision; i++)
            {
                digits[i] += digitBase - 1;
            }
            digits[precision - 1]++;
        }
        public void BorrowDown()
        {
            digits[0]++;
            for (int i = 1; i < precision; i++)
            {
                digits[i] -= digitBase - 1;
            }
            digits[precision - 1]--;
        }
        public void NormalizePositive()
        {
            for(int i = precision - 1; i > 0; i--)
            {
                digits[i - 1] += digits[i] / digitBase;
                digits[i] %= digitBase;
            }
        }
        public void NormalizeNegative()
        {
            for(int i = precision - 1; i > 0; i--)
            {
                if (digits[i] > 0)
                {
                    digits[i - 1] += digits[i] / digitBase;
                }
                else
                {
                    digits[i - 1] -= Math.Abs(digits[i]) / digitBase;
                }

                digits[i] = -(Math.Abs(digits[i]) % digitBase);
            }
        }
        public void Normalize()
        {
            if (IsPositive())
            {
                BorrowUp();
                NormalizePositive();
            }
            else
            {
                BorrowDown();
                NormalizeNegative();
            }
        }
        public void NormalizeNonBorrow()
        {
            if (IsPositive())
            {
                NormalizePositive();
            }
            else
            {
                NormalizeNegative();
            }
        }
        public void Negate()
        {
            for(int i = 0; i < precision; i++)
            {
                digits[i] = -digits[i];
            }
        }
        //Function used in multiplicatacion
        public int PrepareForAdding(int num, int shiftAmount)
        {
            int bonus = 0;
            for (int i = 0; i < precision; i++)
            {
                digits[i] *= num;
            }
            NormalizePositive();
            int[] shifted = new int[precision];
            for (int i = 0; i < precision; i++)
            {
                if (i - shiftAmount >= 0)
                {
                    shifted[i] = digits[i - shiftAmount];
                }
                else
                {
                    shifted[i] = 0;
                }
            }
            if (shiftAmount != 0)
            {
                bonus += digits[precision - shiftAmount];
            }
            digits = shifted;
            NormalizePositive();
            return bonus;
        }
        public double ToDouble()
        {
            double res = 0;
            bool negate = !IsPositive();
            if (negate)
            {
                Negate();
            }
            for (int i = 0; i < precision; i++)
            {
                double multpiplyer = 1;
                for (int x = 0; x < i; x++)
                {
                    multpiplyer /= digitBase;
                }
                res += (double)digits[i] * multpiplyer;
            }
            if (negate)
            {
                Negate();
                res *= -1;
            }
            return res;
        }
        public static bool operator >(FixedPointNumber a, FixedPointNumber b)
        {
            for (int i = 0; i < a.precision; i++)
            {
                if (a.digits[i] > b.digits[i])
                {
                    return true;
                }
                if (a.digits[i] < b.digits[i])
                {
                    return false;
                }
            }
            return false;
        }
        public static bool operator <(FixedPointNumber a, FixedPointNumber b)
        {
            for (int i = 0; i < a.precision; i++)
            {
                if (a.digits[i] < b.digits[i])
                {
                    return true;
                }
                if (a.digits[i] > b.digits[i])
                {
                    return false;
                }
            }
            return false;
        }
        public static FixedPointNumber operator +(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = new(aPassed);
            FixedPointNumber b = new(bPassed);
           
            for(int i = 0; i < a.precision; i++)
            {
                a.digits[i] +=b.digits[i];
            }
            a.Normalize();
            return a;
        }
        public static FixedPointNumber operator *(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = new(aPassed);
            FixedPointNumber b = new(bPassed);
            FixedPointNumber res = new(a.precision);
            FixedPointNumber temp1 = new(a.precision);
            FixedPointNumber temp2 = new(a.precision);

            bool negate = false;
            int bonus = 0;
            for(int i = 0; i < a.precision; i++)
            {
                temp2.digits[i] = 0;
            }
            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                    a.Negate();
                    b.Negate();
                }
                else
                {
                    a.Negate();
                    negate = true;
                }

            }else if (!b.IsPositive())
            {
                b.Negate();
                negate = true;
            }

            for(int i = 0; i < a.precision; i++) { 
                for(int k = 0; k < a.precision; k++)
                {
                    temp1.digits[k] = a.digits[k];
                }
                bonus = temp1.PrepareForAdding(b.digits[i], i);
                temp2 += temp1;
                    
            }
            for(int i = 0; i < a.precision; i++)
            {
                res.digits[i] = temp2.digits[i];
            }
            res.digits[a.precision - 1] += bonus / digitBase;
            if (bonus % digitBase >= digitBase / 2)
            {
                res.digits[a.precision - 1]++;
            }
            if (negate)
            {
                res.Negate();
            }
            return res;

        }
        public static FixedPointNumber operator -(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = new(aPassed);
            FixedPointNumber b = new(bPassed);
            for (int i = 0; i < a.precision; i++)
            {
                a.digits[i] -= b.digits[i];
            }
            a.Normalize();
            return a;
        }

    }



}