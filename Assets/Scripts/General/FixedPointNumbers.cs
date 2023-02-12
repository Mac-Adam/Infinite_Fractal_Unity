namespace FixedPointNumberSystem
{

    public struct FixedPointNumber
    {
        public static int digitBase = 46300;
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
        public void setDouble(double num)
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
            bool res = true;
            for (int i = 0; i < precision; i++)
            {
                if (digits[i] < 0)
                {
                    res = false;
                }
            }
            return res;
        }
        public void Negate()
        {
            for (int i = 0; i < precision; i++)
            {
                if (digits[i] != 0)
                {
                    digits[i] = -digits[i];
                    return;

                }

            }
        }
        public void MultiplyByInt(int num)
        {
            bool negate = false;

            if (!IsPositive())
            {
                if (num < 0)
                {
                    Negate();
                    num = -num;
                }
                else
                {
                    Negate();
                    negate = true;
                }
            }
            else if (num < 0)
            {
                num = -num;
                negate = true;
            }

            for (int i = 0; i < precision; i++)
            {
                digits[i] *= num;
            }

            for (int x = precision - 1; x >= 0; x--)
            {

                if (x != 0)
                {
                    digits[x - 1] += digits[x] / digitBase;
                }
                digits[x] %= digitBase;
            }


            if (negate)
            {
                Negate();
            }
        }
        public void Shift(int num)
        {
            int[] newDigits = new int[precision];
            for (int i = 0; i < precision; i++)
            {
                if (i + num >= 0 && i + num < precision)
                {
                    newDigits[i] = digits[i + num];
                }
                else
                {
                    newDigits[i] = 0;
                }
            }
            digits = newDigits;
        }
        public void Set(FixedPointNumber fpnum)
        {
            for (int i = 0; i < precision; i++)
            {
                if (i < fpnum.precision)
                {
                    digits[i] = fpnum.digits[i];
                }
                else
                {
                    digits[i] = 0;
                }
            }
        }
        public double toDouble()
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
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
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
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
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
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            bool negate = false;

            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                    a.Negate();
                    b.Negate();
                    negate = true;
                }
                else
                {
                    a.Negate();
                    return b - a;
                }
            }
            else if (!b.IsPositive())
            {
                b.Negate();
                return a - b;
            }
            FixedPointNumber res = new FixedPointNumber(a.precision);
            res = a;
            for (int i = 0; i < a.precision; i++)
            {
                res.digits[i] += b.digits[i];

            }
            for (int x = a.precision - 1; x >= 0; x--)
            {

                if (x != 0)
                {
                    res.digits[x - 1] += res.digits[x] / digitBase;
                }
                res.digits[x] %= digitBase;
            }
            if (negate)
            {
                res.Negate();


            }
            return res;
        }
        public static FixedPointNumber operator *(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            bool negate = false;
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
            }
            else if (!b.IsPositive())
            {
                b.Negate();
                negate = true;
            }
            FixedPointNumber multiplicationResults = new FixedPointNumber(a.precision + b.precision);
            FixedPointNumber temp = new FixedPointNumber(a.precision + b.precision);
            FixedPointNumber res = new FixedPointNumber(a.precision);
            for (int i = 0; i < a.precision; i++)
            {
                for (int k = 0; k < a.precision * 2; k++)
                {
                    if (k < a.precision)
                    {
                        temp.digits[k] = a.digits[k];
                    }
                    else
                    {
                        temp.digits[k] = 0;
                    }

                }
                temp.Shift(-i);
                temp.MultiplyByInt(b.digits[i]);
                multiplicationResults += temp;

            }

            for (int x = 0; x < a.precision; x++)
            {
                res.digits[x] = multiplicationResults.digits[x];
            }
            res.digits[a.precision - 1] += multiplicationResults.digits[a.precision] / (digitBase / 2);
            if (negate)
            {
                res.Negate();
            }
            return res;

        }
        public static FixedPointNumber operator -(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            FixedPointNumber res = new FixedPointNumber(a.precision);
            if (!b.IsPositive())
            {
                b.Negate();
                return a + b;
            }
            else if (!a.IsPositive())
            {
                a.Negate();
                res = a + b;
                res.Negate();
                return res;
            }
            if (b > a)
            {
                res = b - a;
                res.Negate();
                return res;
            }
            for (int i = 0; i < a.precision; i++)
            {
                res.digits[i] = a.digits[i] - b.digits[i];
            }
            res.digits[0]--;

            for (int j = 1; j < a.precision; j++)
            {
                res.digits[j] += digitBase - 1;
            }
            res.digits[a.precision - 1]++;
            for (int k = a.precision - 1; k >= 0; k--)
            {

                if (k != 0)
                {
                    res.digits[k - 1] += res.digits[k] / digitBase;
                }
                res.digits[k] %= digitBase;
            }
            return res;
        }
        public FixedPointNumber Replicate()
        {
            FixedPointNumber res = new FixedPointNumber(precision);
            for (int i = 0; i < precision; i++)
            {
                res.digits[i] = digits[i];
            }
            return res;
        }

    }



}