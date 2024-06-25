//This numbering system is a coppy of the fixed point numberign system but it works on floating point numbers
// might replace the fixed point complely but it depents on the performce
#ifndef DIGITBASE
#define DIGITBASE 46300u
#endif

static const uint maxShift = 1000;
#define INFSHIFT -10000 // Returned from UpdateShift when the value is zero


//the accual numeber takes only last 16 bits in each int ( this is because when multiplying 2^16 * 2^16 you can get 2^32 so its the maximum
//usable value for simple computation without overflow
//the amount of shifts is abs(first int) / DIGITBASE - maxShift (could theoreticaly be bigget but (2^(16))^(+/- 1000) is asbsurd enought

//The idea is to decode the addition info befeore the operation, do the shifts, upadte the shift count and encode it back again. 

//Sign is encoded identicaly as it is in the fpNums, that it each int is signed

#define SHITFMASK 0b11111111 11111111 00000000 00000000


//in - function mainly for internal use, digits might need to be prepared first
//ext - safe to use whenever
//ext* not designed to be used outside but should work



//Due to hlsl limitanios, the functions could't have been overloaded, so unfortuneately,
//every time you use those functions you need to remember to use the addf, multiplyf and so on function variants

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
	[fastopt]
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

//used interanlly in multiplying
digits addPositives(digits a, digits b) {
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] += b.digits[i];
	}
	a = normalizePositive(a);
	return a;
}

//ext
digits abs(digits a) {
	if (IsPositive(a)) {
		return a;
	}
	return Negate(a);
}

#define COMMON
#endif

int getShift(digits a) {
	return abs(a.digits[0]) / DIGITBASE - maxShift;
}

int decode(inout digits a) {
	int shift = getShift(a);
	if (a.digits[0] < 0) {
		a.digits[0] += (shift + maxShift) * DIGITBASE;
	}
	else {
		a.digits[0] -= (shift + maxShift) * DIGITBASE;
	}
	
	return shift;
}

void encode(inout digits a, int shift) {
	if (a.digits[0] < 0) {
		a.digits[0] -= DIGITBASE * (shift + maxShift);
	}
	else {
		a.digits[0] += DIGITBASE * (shift + maxShift);
	}
	
}

//used for adding and so on, matches the shifts and returns the common shift
int matchShifts(inout digits a, inout digits b)
{
	int aSh = decode(a);
	int bSh = decode(b);
	int diff = aSh - bSh;

	if (diff == 0) {
		return aSh;
	}
	[unroll]
	for (int i = PRECISION - 1; i >= 0; i--) {
		if (i < abs(diff)) {
			if (diff > 0) {
				b.digits[i] = 0;
			}
			else {
				a.digits[i] = 0;
			}
		}
		else {
			if (diff > 0) {
				b.digits[i] = b.digits[i - diff];
			}
			else {
				a.digits[i] = a.digits[i + diff];
			}

		}
	}
	

	if (diff > 0) {
		return aSh;
	}
	return bSh;

}

//Shifts and returns the sift
//THIS SHOULD BE USED ON DECODED NUMBER, IT DOESN'T ENCODE IT
//BUG IS HERE
int updateShift(inout digits a) {
	uint firstAbs = abs(a.digits[0]);
	if (firstAbs >= DIGITBASE) {
		[unroll]
		for (int i = PRECISION - 1; i > 0; i--) {
			a.digits[i] = a.digits[i - 1];
		}
		int first = firstAbs / DIGITBASE;
		int second = firstAbs % DIGITBASE;
		if (a.digits[0] > 0) {
			a.digits[0] = first;
			a.digits[1] = second;
		}
		else {
			a.digits[0] = -first;
			a.digits[1] = -second;
		}
		
		return 1;
	}
	int counter = 0;
	while (counter < PRECISION && a.digits[counter] == 0) {
		counter++;
		if (counter >= PRECISION) {
			break;
		}
	}
	if (counter == PRECISION) {
		return INFSHIFT;
	}
	if (counter == 0) {
		return 0;
	}
	[unroll]
	for (int j = 0; j < PRECISION; j++) {
		if (j + counter >= PRECISION) {
			a.digits[j] = 0;
		}
		else {
			a.digits[j] = a.digits[j+counter];
		}
	}

	return -counter;
}
digits intMulf(digits a, int num) {
	int temp = decode(a);
	if (num == 0) {
		temp = 0;
	}
	for (int j = 0; j < PRECISION; j++) {
		a.digits[j] *= num;
	}
	a = Normalize(a);
	int uSh = updateShift(a);
	if (uSh == INFSHIFT) {
		temp = 0;
	}
	else {
		temp += uSh;
	}
	encode(a, temp);
	return a;
}
//ext
bool IsGreaterf(digits a, digits b)
{
	int aSh = getShift(a);
	int bSh = getShift(b);
	if (IsPositive(a) != IsPositive(b)) {
		return IsPositive(a);
	}
	if (aSh != bSh) {
		if (IsPositive(a)) {
			return aSh > bSh;
		}
		else {
			return bSh > aSh;
		}
	}
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
//ext
digits subtractf(digits a, digits b) {
	int temp = matchShifts(a, b);
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] -= b.digits[i];

	}
	a = Normalize(a);
	int uSh = updateShift(a);
	if (uSh == INFSHIFT) {
		temp = 0;
	}
	else {
		temp += uSh;
	}
	encode(a, temp);
	return a;
}
//ext
digits addf(digits a, digits b) {
	int temp = matchShifts(a, b);
	[unroll]
	for (int i = 0; i < PRECISION; i++) {
		a.digits[i] += b.digits[i];
	}
	a = Normalize(a);
	int uSh = updateShift(a);
	if (uSh == INFSHIFT) {
		temp = 0;
	}
	else {
		temp += uSh;
	}
	encode(a, temp);
	return a;
}

//ext
digits multiplyf(digits a, digits b)
{
	bool negate = false;
	int aSh = decode(a);
	int bSh = decode(b);
	digits res;
	digits temp1;
	digits temp2;
	int bonus = 0;
	[unroll]
	for (int j = 0; j < PRECISION; j++) {
		temp2.digits[j] = 0;
	}
	//Match signs
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

	//multiply
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
	//additional precision
	res.digits[PRECISION - 1] += bonus / DIGITBASE;
	if (bonus % DIGITBASE >= DIGITBASE / 2) {
		res.digits[PRECISION - 1]++;
	}
	//fix signs
	if (negate) {
		res = Negate(res);
	}
	int uSh = updateShift(res);
	int finalShift = 0;
	if (uSh != INFSHIFT) {
		finalShift = aSh + bSh + uSh;
	}	
	encode(res, finalShift);
	return res;
}
//ext
digits squaref(digits a) {
	return multiplyf(a, a);
}

//ext
digits setDoublef(double num) {
	digits a;
	int shift = 0;
	if (num != 0) {
		shift = int(floor(log(num) / log(DIGITBASE)));
	}
	bool negate = false;
	if (num < 0) {
		negate = true;
		num = -num;
	}
	double temp = num;
	
	temp /= pow(DIGITBASE, shift);
	[unroll]
	for (int i = 0; i < PRECISION; i++)
	{
		a.digits[i] = (int)temp;
		temp = (temp - a.digits[i]) * DIGITBASE;
	}
	if (negate) {
		a = Negate(a);
	}
	encode(a, shift);
	return a;
}
float toFloatf(digits num){
	float res = 0.0;
	int shift = decode(num);
	for (int i = 0; i < PRECISION; i++) {
		res += num.digits[i] * pow(DIGITBASE,-i);
	}
	encode(num, shift);
	return res*pow(DIGITBASE,shift);
}

double toDoublef(digits num) {
	double res = 0.0;
	int shift = decode(num);
	for (int i = 0; i < PRECISION; i++) {
		res += num.digits[i] * pow(DIGITBASE, -i);
	}
	encode(num, shift);
	return res * pow(DIGITBASE, shift);
}

digits fromFP(digits a) {
	digits res;
	for (int i = 0; i < PRECISION; i++) {
		res.digits[i] = a.digits[i];
	}
	int temp = updateShift(res);
	encode(res, temp);
	return res;
}

complex mulComplexf(complex a, complex b) {
	complex res;
	res.x = subtractf(multiplyf(a.x, b.x), multiplyf(a.y, b.y));
	res.y = addf(multiplyf(a.x, b.y), multiplyf(a.y, b.x));
	return res;
}




