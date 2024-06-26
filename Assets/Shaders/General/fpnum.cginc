//PRECISION can't be #pragma multi_compile'ed here
//Define it in the file you are including it in

#ifndef DIGITBASE
#define DIGITBASE 46300u
#endif

//Those fucntions are identical in both files, so they dont have to be duplicated
#ifndef COMMON
struct digits {
	int digits[PRECISION];
};

struct complex {
	digits x;
	digits y;
};


bool IsPositive(digits a) {
	
	for (int i = 0; i < PRECISION; i++) {
		if (a.digits[i] != 0) {
			return a.digits[i] > 0;
		}
	}
	return true;
}
digits borrowUp(digits a) {
	a.digits[0]--;
	[unroll]
	for (int j = 1; j < PRECISION; j++) {
		a.digits[j] += DIGITBASE - 1;
	}
	a.digits[PRECISION - 1]++;
	return a;
}
digits borrowDown(digits a) {
	a.digits[0]++;
	[unroll]
	for (int j = 1; j < PRECISION; j++) {
		a.digits[j] -= DIGITBASE - 1;
	}
	a.digits[PRECISION - 1]--;
	return a;
}

//All digits has to be the same sign
digits normalizePositive(digits a) {
	[unroll]
	for (int x = PRECISION - 1; x > 0; x--) {
		a.digits[x - 1] += a.digits[x] / DIGITBASE;
		a.digits[x] %= DIGITBASE;
	}
	return a;
}
digits normalizeNegative(digits a) {
	[unroll]
	for (int x = PRECISION - 1; x > 0; x--) {
		if (a.digits[x] > 0) {
			a.digits[x - 1] += a.digits[x] / DIGITBASE;
		}
		else {
			a.digits[x - 1] -= abs(a.digits[x]) / DIGITBASE;
		}

		a.digits[x] = -(int)(abs(a.digits[x]) % DIGITBASE);
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

digits PrepareForAdding(digits a, inout int bonus, int num, int shiftAmount) {//Multiplies shifts and normalizes
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

digits addPositives(digits a, digits b) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] += b.digits[i];
	}
	a = normalizePositive(a);
	return a;
}

digits abs(digits a) {
	if (IsPositive(a)) {
		return a;
	}
	return Negate(a);
}

#define COMMON
#endif




digits intMul(digits a, int num) {
	
	for (int j = 0; j < PRECISION; j++) {
		a.digits[j] *= num;
	}
	return Normalize(a);
}


bool IsGreater(digits a, digits b)
{
	
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
	res.digits[PRECISION - 1] += bonus / DIGITBASE;
	if (bonus % DIGITBASE >= DIGITBASE / 2) {
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
		temp = (temp - a.digits[i]) * DIGITBASE;
	}
	if (negate) {
		a = Negate(a);
	}
	return a;
}
bool inBounds(digits a, digits b, int r) {
	a = add(a, b);
	[unroll]
	for (int c = 0; c < PRECISION; c++) {
		b.digits[c] = 0;
	}
	b.digits[0] = r;
	return IsGreater(b, a);
}
float toFloat(digits num){
	float res = 0.0;
	
	for (int i = 0; i < PRECISION; i++) {
		res += num.digits[i] * pow(DIGITBASE,-i);
	}
	return res;
}

double toDouble(digits num) {
	double res = 0.0;
	
	for (int i = 0; i < PRECISION; i++) {
		res += num.digits[i] * pow(DIGITBASE, -i);
	}
	return res;
}

complex mulComplex(complex a, complex b) {
	complex res;
	res.x = subtract(multiply(a.x, b.x), multiply(a.y, b.y));
	res.y = add(multiply(a.x, b.y), multiply(a.y, b.x));
	return res;
}




