namespace CommonFunctions
{
    class MathFunctions
    {
        public static int IntPow(int baseNum, int exponent)
        {
            int res;
            if (exponent == 0)
            {
                res = 1;
            }
            else
            {
                res = baseNum;
            }
            for (int i = 1; i < exponent; i++)
            {
                res *= baseNum;
            }
            return res;
        }
    }
}