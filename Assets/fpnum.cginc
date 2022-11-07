
static const int fpPre = 3;
static const uint digitBase = 46300;


struct digits {
	int digits[fpPre];
};
struct digitsDP {
	int digits[fpPre * 2];
};

bool IsPositive(digits a) {
	bool res = true;
	[fastopt]
	for (int i = 0; i < fpPre; i++) {
		if (a.digits[i] < 0) {
			res = false;
		}
	}
	return res;
}
digits Negate(digits a) {
	bool done = false;
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		if (a.digits[i] != 0 && !done) {
			a.digits[i] = -a.digits[i];
			done = true;

		}

	}
	return a;
}
digits MultiplyByInt(digits a, int num) {
	bool negate = false;
	if (!IsPositive(a))
	{
		if (num < 0)
		{
			a = Negate(a);
			num = -num;
		}
		else
		{
			a = Negate(a);
			negate = true;
		}
	}
	else if (num < 0) {
		num = -num;
		negate = true;
	}
	[unroll]
	for (int i = 0; i < fpPre; i++) {
		a.digits[i] *= num;
	}
	[unroll]
	for (int x = fpPre - 1; x >= 0; x--) {

		if (x != 0)
		{
			a.digits[x - 1] += a.digits[x] / digitBase;
		}
		a.digits[x] %= digitBase;
	}


	if (negate) {
		a = Negate(a);
	}
	return a;
}
digits  Shift(digits a, int num) {
	digits res;
	bool negate = false;
	if (!IsPositive(a)) {
		a = Negate(a);
		negate = true;
	}
	[fastopt]
	for (int i = 0; i < fpPre; i++)
	{
		if (i + num >= 0 && i + num < fpPre)
		{
			res.digits[i] = a.digits[i + num];
		}
		else {
			res.digits[i] = 0;
		}
	}
	if (negate) {
		res = Negate(res);
	}

	return res;
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
	[unroll]
	for (int k = fpPre - 1; k >= 0; k--) {

		if (k != 0)
		{
			res.digits[k - 1] += res.digits[k] / digitBase;
		}
		res.digits[k] %= digitBase;
	}
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
	[unroll]
	for (int x = fpPre - 1; x >= 0; x--) {

		if (x != 0)
		{
			res.digits[x - 1] += res.digits[x] / digitBase;
		}
		res.digits[x] %= digitBase;
	}
	if (negate)
	{
		res = Negate(res);

	}

	return res;
}
bool isGraterDP(digitsDP a, digitsDP b)
{
	[fastopt]
	for (int i = 0; i < fpPre * 2; i++) {
		if (a.digits[i] > b.digits[i])
		{
			return true;
		}if (a.digits[i] < b.digits[i]) {
			return false;
		}
	}
	return false;
}

bool isGraterAbsDP(digitsDP a, digitsDP b)
{
	[fastopt]
	for (int i = 0; i < fpPre * 2; i++) {
		if (abs(a.digits[i]) > abs(b.digits[i]))
		{
			return true;
		}if (abs(a.digits[i]) < abs(b.digits[i])) {
			return false;
		}
	}
	return false;
}
bool IsPositiveDP(digitsDP a) {
	bool res = true;
	[fastopt]
	for (int i = 0; i < fpPre * 2; i++) {
		if (a.digits[i] < 0) {
			res = false;
		}
	}
	return res;
}

digitsDP NegateDP(digitsDP a) {
	bool done = false;
	[unroll]
	for (int i = 0; i < fpPre * 2; i++) {
		if (a.digits[i] != 0 && !done) {
			a.digits[i] = -a.digits[i];
			done = true;

		}

	}
	return a;
}
digitsDP  absFpNumDP(digitsDP a) {
	[unroll]
	for (int i = 0; i < fpPre * 2; i++) {
		a.digits[i] = abs(a.digits[i]);
	}
	return a;

}

digitsDP  ShiftDP(digitsDP a, int num) {
	digitsDP res;
	bool negate = false;
	if (!IsPositiveDP(a)) {
		a = NegateDP(a);
		negate = true;
	}
	[unroll]
	for (int i = 0; i < fpPre * 2; i++)
	{
		if (i + num >= 0 && i + num < fpPre * 2)
		{
			res.digits[i] = a.digits[i + num];
		}
		else {
			res.digits[i] = 0;
		}
	}
	if (negate) {
		res = NegateDP(res);
	}

	return res;
}
digitsDP MultiplyByIntDP(digitsDP a, int num) {

	bool negate = false;
	if (!IsPositiveDP(a))
	{
		if (num < 0)
		{
			a = NegateDP(a);
			num = -num;
		}
		else
		{
			a = NegateDP(a);
			negate = true;
		}
	}
	else if (num < 0) {
		num = -num;
		negate = true;
	}
	[unroll]
	for (int i = 0; i < fpPre * 2; i++) {
		a.digits[i] *= num;
	}
	[unroll]
	for (int x = fpPre * 2 - 1; x >= 0; x--) {

		if (x != 0)
		{
			a.digits[x - 1] += a.digits[x] / digitBase;
		}
		a.digits[x] %= digitBase;
	}


	if (negate) {
		a = NegateDP(a);
	}
	return a;
}


digitsDP subDP(digitsDP a, digitsDP b)
{
	digitsDP res;
	[unroll]
	for (int i = 0; i < fpPre * 2; i++) {
		res.digits[i] = a.digits[i] - b.digits[i];
	}
	res.digits[0]--;
	[unroll]
	for (int j = 1; j < fpPre * 2; j++) {
		res.digits[j] += digitBase - 1;
	}
	res.digits[fpPre * 2 - 1]++;
	[unroll]
	for (int k = fpPre * 2 - 1; k >= 0; k--) {

		if (k != 0)
		{
			res.digits[k - 1] += res.digits[k] / digitBase;
		}
		res.digits[k] %= digitBase;
	}

	return res;

}
digitsDP addDP(digitsDP a, digitsDP b) {
	bool negate = false;
	digitsDP res;
	if (!IsPositiveDP(a) && !IsPositiveDP(b))
	{
		a = NegateDP(a);
		b = NegateDP(b);
		negate = true;

	}
	else if (!IsPositiveDP(a) && IsPositiveDP(b)) {
		//b-a
		if (isGraterAbsDP(b, a)) {
			a = absFpNumDP(a);
			res = subDP(b, a);
			a = NegateDP(a);
			return res;
		}
		else {
			a = absFpNumDP(a);
			res = subDP(a, b);
			res = NegateDP(res);
			a = NegateDP(a);
			return res;
			//reverse
		}




	}
	else if (!IsPositiveDP(b) && IsPositiveDP(a)) {
		//a-b
		if (isGraterAbsDP(a, b)) {
			b = absFpNumDP(b);

			res = subDP(a, b);
			b = NegateDP(b);
			return res;
		}
		else {
			//reverse
			b = absFpNumDP(b);
			res = subDP(b, a);
			res = NegateDP(res);
			b = NegateDP(b);
			return res;
		}

	}

	res = a;
	[unroll]
	for (int i = 0; i < fpPre * 2; i++) {
		res.digits[i] += b.digits[i];

	}
	[unroll]
	for (int x = fpPre * 2 - 1; x >= 0; x--) {

		if (x != 0)
		{
			res.digits[x - 1] += res.digits[x] / digitBase;
		}
		res.digits[x] %= digitBase;
	}
	if (negate)
	{
		res = NegateDP(res);

	}

	return res;
}
digits multiply(digits a, digits b)
{
	bool negate = false;
	bool negatea = false;
	bool negateb = false;
	digits res;
	digitsDP temp1;
	digitsDP temp2;

	[unroll]
	for (int j = 0; j < fpPre * 2; j++) {
		temp2.digits[j] = 0;

	}
	if (!IsPositive(a))
	{
		if (!IsPositive(b))
		{
			a = Negate(a);
			negatea = true;
			b = Negate(b);
			negateb = true;
		}
		else
		{
			a = Negate(a);
			negatea = true;
			negate = true;
		}
	}
	else if (!IsPositive(b)) {
		b = Negate(b);
		negateb = true;
		negate = true;
	}
	[fastopt]
	for (int i = 0; i < fpPre; i++) {
		for (int k = 0; k < fpPre * 2; k++) {
			if (k < fpPre) {
				temp1.digits[k] = a.digits[k];
			}
			else {
				temp1.digits[k] = 0;
			}

		}

		temp1 = ShiftDP(temp1, -i);
		temp1 = MultiplyByIntDP(temp1, b.digits[i]);
		temp2 = addDP(temp1, temp2);
	}
	if (negatea) {
		a = Negate(a);
	}
	if (negateb)
	{
		b = Negate(b);
	}
	[unroll]
	for (int x = 0; x < fpPre; x++) {
		res.digits[x] = temp2.digits[x];
	}
	res.digits[fpPre - 1] += temp2.digits[fpPre] / (digitBase / 2);
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


