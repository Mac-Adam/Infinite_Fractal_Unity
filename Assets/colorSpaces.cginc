
static const float3 ILLUMINANT = float3(95.0489, 100.0, 108.8840);
static const float PI = 3.14159265359;

float3 lab2xyz(float3 c) {

	float var_Y = (c.x +16.0) / 116.0;
	float var_X = c.y / 500.0 + var_Y;
	float var_Z = var_Y - c.z / 200.0;
	if (pow(var_Y , 3.0) > 0.008856) { 
		var_Y = pow(var_Y , 3.0);
	}
	else { 
		var_Y = (var_Y - 16.0 / 116.0) / 7.787;
	}
	if (pow(var_X,3.0) > 0.008856) {
		var_X = pow(var_X, 3.0);
	}
	else {
		var_X = (var_X - 16.0 / 116.0) / 7.787;
	}
	if (pow(var_Z , 3.0) > 0.008856) {
		var_Z = pow(var_Z, 3.0);
	}
	else {
		var_Z = (var_Z - 16.0 / 116.0) / 7.787;
	}

	float X = var_X * ILLUMINANT.x;
	float Y = var_Y * ILLUMINANT.y;
	float Z = var_Z * ILLUMINANT.z;
	return float3(X, Y, Z);
}
float3 xyz2lab(float3 c) { 
	float var_X = c.x / ILLUMINANT.x;
	float var_Y = c.y / ILLUMINANT.y;
	float var_Z = c.z / ILLUMINANT.z;
	if (var_X > 0.008856) {
		var_X = pow(abs(var_X) ,(1.0/3.0));
	}
	else {
		var_X = (7.787 * var_X) + (16.0 / 116.0);
	}
	if (var_Y > 0.008856) {
		var_Y = pow(abs(var_Y), (1.0 / 3.0));
	}
	else {
		var_Y = (7.787 * var_Y) + (16.0 / 116.0);
	}
	if (var_Z > 0.008856) {
		var_Z = pow(abs(var_Z), (1.0 / 3.0));
	}
	else {
		var_Z = (7.787 * var_Z) + (16.0 / 116.0);
	}

	float L = (116.0 * var_Y) - 16.0;
	float a = 500.0 * (var_X - var_Y);
	float b = 200.0 * (var_Y - var_Z);
	return float3(L, a, b);

}
float3 rgb2xyz(float3 c) { 

	float var_R = c.x;
	float var_G = c.y ;
	float var_B = c.z ;
	if (var_R > 0.04045) {
		var_R = pow((abs(var_R + 0.055) / 1.055), 2.4);
	}
	else
	{
		var_R = var_R / 12.92;
	}
	if (var_G > 0.04045) {
		var_G = pow(abs((var_G + 0.055) / 1.055), 2.4);
	}
	else
	{
		var_G = var_G / 12.92;
	}
	if (var_B > 0.04045) {
		var_B = pow((abs(var_B + 0.055) / 1.055), 2.4);
	}
	else
	{
		var_B = var_B / 12.92;
	}

	var_R = var_R * 100.0;
	var_G = var_G * 100.0;
	var_B = var_B * 100.0;
	float X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
	float Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
	float Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;
	return float3(X, Y, Z);

}
float3 xyz2rgb(float3 c) {
	float var_X = c.x / 100.0;
	float var_Y = c.y / 100.0;
	float var_Z = c.z / 100.0;

	float var_R = var_X * 3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
	float var_G = var_X * -0.9689 + var_Y * 1.8758 + var_Z * 0.0415;
	float var_B = var_X * 0.0557 + var_Y * -0.2040 + var_Z * 1.0570;

	if (var_R > 0.0031308) {
		var_R = 1.055 * (pow(abs(var_R) , (1 / 2.4))) - 0.055;
	}
	else {
		var_R = 12.92 * var_R;
	}                  
	if (var_G > 0.0031308) {

		var_G = 1.055 * (pow(abs(var_G) ,(1 / 2.4))) - 0.055;
	}
	else {
		var_G = 12.92 * var_G;
	}
	if (var_B > 0.0031308) {
		var_B = 1.055 * (pow(abs(var_B) , (1 / 2.4))) - 0.055;
	}
	else {
		var_B = 12.92 * var_B;
	}                
	float sR = var_R;
	float sG = var_G;
	float sB = var_B;
	return float3(sR, sG, sB);
}
float3 lab2lch(float3 c) {
	float var_H;
	if (c.y == 0) {
		var_H = PI / 2.0;
	}
	else {
		var_H = atan(abs(c.z/c.y));
	}
	if (c.y > 0 && c.z > 0) {
		var_H = 180.0 * var_H / PI;
	}
	else if (c.y < 0 && c.z>0) {
		var_H = 180.0  - 180.0 * var_H / PI;
	}
	else if (c.y < 0 && c.z<0) {
		var_H = 180.0 + 180.0 * var_H / PI;
	}
	else{
		var_H = 360.0 - 180.0 * var_H / PI;
	}
	float L = c.x;
	float C = sqrt(c.y * c.y + c.z * c.z);
	float H = var_H;
	return float3(L, C, H);
}
float3 lch2lab(float3 c) {
	return float3(
		c.x,
		cos(c.z * 2 * PI / 360.0) * c.y,
		sin(c.z * 2 * PI / 360.0) * c.y
		);
}
float3 rgb2lab(float3 c) {
    return xyz2lab(rgb2xyz(c));
}

float3 lab2rgb(float3 c) {
    return xyz2rgb(lab2xyz(c));
}
float3 rgb2lch(float3 c) {
	return lab2lch(rgb2lab(c));
}
float3 lch2rgb(float3 c) {
	return lab2rgb(lch2lab(c));
}
float4 lerpColors(float4 c0, float4 c1, float t, int type,bool cubic) {
	float4 color = c0;
	if (cubic) {
		t = -2.0 * pow(t, 3) + 3.0 * pow(t, 2);
	}
	if (type == 0) {
		color = c0 + (c1 - c0) * t;
	}
	else if (type == 1) {
		c0.xyz = rgb2lab(c0.xyz);
		c1.xyz = rgb2lab(c1.xyz);
		color = float4(lab2rgb( c0.xyz + (c1.xyz - c0.xyz) * t ),1.0);
	}
	else if (type == 2) {
		c0.xyz = rgb2lch(c0.xyz);
		c1.xyz = rgb2lch(c1.xyz);
		float2 lc = c0.xy + (c1.xy - c0.xy) * t;
		float angle;
		if (abs(c0.z - c1.z) < 180.0) {
			angle = c0.z + (c1.z - c0.z) * t;
		}
		else {
			if (c0.z < c1.z) {
				c0.z += 360.0;
			}
			else {
				c1.z += 360.0;
			}
			angle = (c0.z + (c1.z - c0.z) * t)%360;
		}
		c0.xyz = lch2rgb(float3(lc, angle));
		color = float4(c0);
	}
	else if (type == 3) {
		c0.xyz = rgb2lch(c0.xyz);
		c1.xyz = rgb2lch(c1.xyz);
		float2 lc = c0.xy + (c1.xy - c0.xy) * t;
		float angle;
		
		if (c0.z > c1.z) {
			c1.z += 360.0;
		}
		angle = (c0.z + (c1.z - c0.z) * t) % 360;
	
		c0.xyz = lch2rgb(float3(lc, angle));
		color = float4(c0);
	}
	else if (type == 4) {
		c0.xyz = rgb2lch(c0.xyz);
		c1.xyz = rgb2lch(c1.xyz);
		float2 lc = c0.xy + (c1.xy - c0.xy) * t;
		float angle;
		
		if (c0.z < c1.z) {
			c0.z += 360.0;
		}
		angle = (c0.z + (c1.z - c0.z) * t) % 360;
		
		c0.xyz = lch2rgb(float3(lc, angle));
		color = float4(c0);
	}
	return color;;
}