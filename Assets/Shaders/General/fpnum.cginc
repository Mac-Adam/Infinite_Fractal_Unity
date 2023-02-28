//Due to some unity bug the PRECISION can't be #pragma multi_compile'ed here
//Define it in the file you are including it in

static const uint digitBase = 46300;



struct digits {
	int digits[PRECISION];
}; 
bool IsPositive(digits a) {
	[fastopt]
	for (int i = 0; i < PRECISION; i++) {
		if (a.digits[i] != 0) {
			return a.digits[i]>0;
		}
	}
	return true;
}
digits borrowUp(digits a) {
	a.digits[0]--;
	[unroll]
	for (int j = 1; j < PRECISION; j++) {
		a.digits[j] += digitBase - 1;
	}
	a.digits[PRECISION - 1]++;
	return a;
}
digits borrowDown(digits a) {
	a.digits[0]++;
	[unroll]
	for (int j = 1; j < PRECISION; j++) {
		a.digits[j] -= digitBase - 1;
	}
	a.digits[PRECISION - 1]--;
	return a;
}
//All digits has to be the same sign
digits normalizePositive(digits a) {
	[unroll]
	for (int x = PRECISION - 1; x > 0; x--) {
		a.digits[x - 1] += a.digits[x] / digitBase;
		a.digits[x] %= digitBase;
	}
	return a;
}
digits normalizeNegative(digits a) {
	[unroll]
	for (int x = PRECISION - 1; x > 0; x--) {
		if (a.digits[x] > 0) {
			a.digits[x - 1] += a.digits[x] / digitBase;
		}
		else {
			a.digits[x - 1] -= abs(a.digits[x]) / digitBase;
		}
		
		a.digits[x] = -(int)(abs(a.digits[x]) % digitBase);
	}
	return a;
}
digits Normalize(digits a) {
	if (IsPositive(a)) {
		a = borrowUp(a);
		a = normalizePositive(a);
	}
	else {
		a = borrowDown(a);
		a = normalizeNegative(a);
	}
	return a;
}
digits NormalizeNonBorrow(digits a) {
	if (IsPositive(a)) {
		a = normalizePositive(a);
	}
	else {
		a = normalizeNegative(a);
	}
	return a;
}

digits Negate(digits a) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] = -a.digits[i];
	}
	return a;
}
digits PrepareForAdding(digits a, inout int bonus, int num,int shiftAmount) {//Multiplies shifts and normalizes
	[unroll]
	for (int j = 0; j < PRECISION; j++) {
		a.digits[j] *= num;
	}
	a = normalizePositive(a);
	digits shifted;
	[unroll]
	for (int i = 0; i < PRECISION; i++)
	{
		if (i - shiftAmount >= 0)
		{
			shifted.digits[i] = a.digits[i - shiftAmount];
		}
		else {
			shifted.digits[i] = 0;
		}
	}
	if (shiftAmount != 0) {
		bonus += a.digits[PRECISION - shiftAmount];
	}
	a = normalizePositive(shifted);

	return a;
}
bool IsGreater(digits a, digits b)
{
	[fastopt]
	for (int i = 0; i < PRECISION; i++) {
		if (a.digits[i] > b.digits[i])
		{
			return true;
		}if (a.digits[i] < b.digits[i]) {
			return false;
		}
	}
	return false;
}
digits subtract(digits a, digits b) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] -= b.digits[i];

	}
	a = Normalize(a);
	return a;
}
digits add(digits a, digits b) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] += b.digits[i];
	}
	a = Normalize(a);
	return a;
}
digits addPositives(digits a, digits b) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] += b.digits[i];
	}
	a = normalizePositive(a);
	return a;
}

digits multiply(digits a, digits b)
{
	bool negate = false;
	digits res;
	digits temp1;
	digits temp2;
	int bonus = 0;
	[unroll]
	for (int j = 0; j < PRECISION; j++) {
		temp2.digits[j] = 0;
	}
	if (!IsPositive(a))
	{
		if (!IsPositive(b))
		{
			a = Negate(a);		
			b = Negate(b);
		}
		else
		{
			a = Negate(a);
			negate = true;
		}
	}
	else if (!IsPositive(b)) {
		b = Negate(b);
		negate = true;
	}
	[fastopt]
	for (int i = 0; i < PRECISION; i++) {
		[unroll]
		for (int k = 0; k < PRECISION; k++) {
			temp1.digits[k] = a.digits[k];		
		}
		temp1 = PrepareForAdding(temp1,bonus, b.digits[i],i);
		temp2 = addPositives(temp1, temp2);
	}
	[unroll]
	for (int x = 0; x < PRECISION; x++) {
		res.digits[x] = temp2.digits[x];
	}
	res.digits[PRECISION - 1] += bonus / digitBase;
	if (bonus % digitBase >= digitBase / 2) {
		res.digits[PRECISION - 1]++;
	}
	if (negate) {
		res = Negate(res);
	}
	return res;
}
digits square(digits a) {
	return multiply(a, a);
}
digits setDouble(double num) {
	digits a;
	bool negate = false;
	if (num < 0) {
		negate = true;
		num = -num;
	}
	double temp = num;
	[unroll]
	for (int i = 0; i < PRECISION; i++)
	{
		a.digits[i] = (int)temp;
		temp = (temp - a.digits[i]) * digitBase;
	}
	if (negate) {
		a = Negate(a);
	}
	return a;
}
bool inBounds(digits a, digits b) {
	a = add(a, b);
	for (int c = 0; c < PRECISION; c++) {
		b.digits[c] = 0;
	}
	b.digits[0] = 4;
	return IsGreater(b, a);
}
float toFloat(digits num){
	float res = 0.0;
	for (int i = 0; i < PRECISION; i++) {
		res += num.digits[i] * pow(digitBase,-i);
	}
	return res;
}


