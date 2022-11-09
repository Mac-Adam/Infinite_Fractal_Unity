
static const int fpPre = 4;
static const uint digitBase = 46300;


struct digits {
	int digits[fpPre];
};

digits Normalize(digits a) {
	[unroll]
	for (int x = fpPre - 1; x >= 0; x--) {

		if (x != 0)
		{
			a.digits[x - 1] += a.digits[x] / digitBase;
			a.digits[x] %= digitBase;
		}
		
	}
	return a;
}

bool IsPositive(digits a) {
	[fastopt]
	for (int i = 0; i < fpPre; i++) {
		if (a.digits[i] < 0) {
			return false;
		}
	}
	return true;
}
digits Negate(digits a) {
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		if (a.digits[i] != 0) {
			a.digits[i] = -a.digits[i];
			break;
		}
	}
	return a;
}
digits PrepareForAdding(digits a, inout int bonus, int num,int shiftAmount) {//Multiplies shifts and normalizes
	[unroll]
	for (int j = 0; j < fpPre; j++) {
		a.digits[j] *= num;
	}
	a = Normalize(a);
	digits shifted;
	[unroll]
	for (int i = 0; i < fpPre; i++)
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
		bonus += shifted.digits[fpPre - shiftAmount];
	}
	a = Normalize(shifted);

	return a;
}
bool isGrater(digits a, digits b)
{
	[fastopt]
	for (int i = 0; i < fpPre; i++) {
		if (a.digits[i] > b.digits[i])
		{
			return true;
		}if (a.digits[i] < b.digits[i]) {
			return false;
		}
	}
	return false;
}

bool isGraterAbs(digits a, digits b)
{
	[fastopt]
	for (int i = 0; i < fpPre; i++) {
		if (abs(a.digits[i]) > abs(b.digits[i]))
		{
			return true;
		}if (abs(a.digits[i]) < abs(b.digits[i])) {
			return false;
		}
	}
	return false;
}



digits absFpNum(digits a) {
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		a.digits[i] = abs(a.digits[i]);
	}

	return a;
}

digits sub(digits a, digits b)
{
	digits res;
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		res.digits[i] = a.digits[i] - b.digits[i];
	}
	res.digits[0]--;
	[unroll]
	for (int j = 1; j < fpPre; j++) {
		res.digits[j] += digitBase - 1;
	}
	res.digits[fpPre - 1]++;
	res = Normalize(res);
	return res;

}

digits add(digits a, digits b) {
	bool negate = false;
	digits res;
	if (!IsPositive(a) && !IsPositive(b))
	{
		a = Negate(a);
		b = Negate(b);
		negate = true;
	}
	else if (!IsPositive(a) && IsPositive(b)) {
		//b-a
		if (isGraterAbs(b, a)) {
			a = absFpNum(a);
			res = sub(b, a);
			a = Negate(a);
			return res;
		}
		else {
			a = absFpNum(a);
			res = sub(a, b);
			res = Negate(res);
			a = Negate(a);
			return res;
			//reverse
		}
	}
	else if (!IsPositive(b) && IsPositive(a)) {
		//a-b
		if (isGraterAbs(a, b)) {
			b = absFpNum(b);
			res = sub(a, b);
			b = Negate(b);
			return res;
		}
		else {
			//reverse
			b = absFpNum(b);
			res = sub(b, a);
			res = Negate(res);
			b = Negate(b);
			return res;
		}

	}

	res = a;
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		res.digits[i] += b.digits[i];

	}
	res = Normalize(res);
	if (negate)
	{
		res = Negate(res);

	}

	return res;
}

digits multiply(digits a, digits b)
{
	bool negate = false;
	digits res;
	digits temp1;
	digits temp2;
	int bonus = 0;
	[unroll]
	for (int j = 0; j < fpPre; j++) {
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
	for (int i = 0; i < fpPre; i++) {
		[unroll]
		for (int k = 0; k < fpPre; k++) {
			temp1.digits[k] = a.digits[k];		
		}
		temp1 = PrepareForAdding(temp1,bonus, b.digits[i],i);
		temp2 = add(temp1, temp2);
	}
	[unroll]
	for (int x = 0; x < fpPre; x++) {
		res.digits[x] = temp2.digits[x];
	}
	res.digits[fpPre - 1] += bonus / digitBase;
	if (bonus % digitBase >= digitBase / 2) {
		res.digits[fpPre - 1]++;
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
	for (int i = 0; i < fpPre; i++)
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

	bool res = true;

	if (a.digits[0] > 4) {
		res = false;
	}
	else if (a.digits[0] == 4) {
		[unroll]
		for (int i = 1; i < fpPre; i++) {
			if (a.digits[i] > 0)
			{
				res = false;
			}

		}
	}
	return res;

}


