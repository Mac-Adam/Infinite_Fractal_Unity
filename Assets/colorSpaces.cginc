static const float labFOffset = 4.0 / 29.0;
static const float delta = 6.0 / 29.0;
static const float delta2 = pow(delta,2);
static const float delta3 = pow(delta,3);

static const float3 Illuminant = float3(95.0489, 100.0, 108.8840);
static const float4 xyzlabConvConstanst = float4(116.0, 500.0, 200.0, 16.0);

static const float xyzConvTreshold = 0.04045;
static const float3 xyzConvConst = float3(12.92, 0.055, 2.4);

static const float xyzInvConvTreshold = 0.0031308;

static const float3x3 xyzMat = float3x3(
	0.4124564, 0.3575761, 0.1804375,
	0.2126729, 0.7151522, 0.0721750,
	0.0193339, 0.1191920, 0.9503041
	);
static const float3x3 xyzInvMat = float3x3(
	3.2404542, -1.5371385, -0.4985314,
	-0.9692660,  1.8760108,  0.0415560,
	0.0556434, -0.2040259,  1.0572252
	);
float labF(float t) {
	if (t > delta3) {
		return pow(abs(t), 1.0 / 3.0);
	}
	return (t / (3 * delta2) + labFOffset);
}
float labFInv(float t) {
	if (t > delta) {
		return pow(t, 3.0);
	}
	return 3 * delta2 * (t - labFOffset);
}
float xyzF(float t) {
	if (t < xyzConvTreshold) {
		return t / xyzConvConst.x;
	}
	return pow(abs((t + xyzConvConst.y) / (1 + xyzConvConst.y)), xyzConvConst.z);
}
float xyzFInv(float t) {
	if (t < xyzInvConvTreshold) {
		return t * xyzConvConst.x;
	}
	return(pow(abs(t), 1 / xyzConvConst.z) * (1 + xyzConvConst.y) - xyzConvConst.y);
}
float3 lab2xyz(float3 c) {
	return float3(
		Illuminant.x * labFInv((c.x + xyzlabConvConstanst.w) / xyzlabConvConstanst.x + c.y / xyzlabConvConstanst.y),
		Illuminant.y * labFInv((c.x + xyzlabConvConstanst.w) / xyzlabConvConstanst.x),
		Illuminant.z * labFInv((c.x + xyzlabConvConstanst.w) / xyzlabConvConstanst.x - c.z / xyzlabConvConstanst.z)
		);
}
float3 xyz2lab(float3 c) {
	float3 normalizedC = c / Illuminant;
	return float3(
		xyzlabConvConstanst.x * labF(normalizedC.y) - xyzlabConvConstanst.w,
		xyzlabConvConstanst.y * (labF(normalizedC.x) - labF(normalizedC.y)),
		xyzlabConvConstanst.z * (labF(normalizedC.y) - labF(normalizedC.z))
		);

}
float3 rgb2xyz(float3 c) { 
	float3 temp = float3(
		xyzF(c.x),
		xyzF(c.y),
		xyzF(c.z)
	);
	return 100.0*mul(temp, xyzMat);
}
float3 xyz2rgb(float3 c) { 
	float3 temp = mul(c / 100.0, xyzInvMat);
	return float3(
		xyzFInv(temp.x),
		xyzFInv(temp.y),
		xyzFInv(temp.z)
	);
}
float3 rgb2lab(float3 c) {
    float3 lab = xyz2lab(rgb2xyz(c));
    return float3(
		lab.x / 100.0,
		0.5 + 0.5 * (lab.y / 127.0),
		0.5 + 0.5 * (lab.z / 127.0)
	);
}



float3 lab2rgb(float3 c) {
    return xyz2rgb(lab2xyz(float3(
		100.0 * c.x,
		2.0 * 127.0 * (c.y - 0.5),
		2.0 * 127.0 * (c.z - 0.5)
	)));
}
float3 lab2lch(float3 c) {
	return float3(
		c.x,
		sqrt(c.y*c.y+c.z*c.z),
		atan(c.z/c.y)
		);
}
float3 lch2lab(float3 c) {
	return float3(
		c.x,
		c.y * cos(c.z),
		c.y * sin(c.z)
		);
}
float3 rgb2lch(float3 c) {
	return lab2lch(rgb2lab(c));
}
float3 lch2rgb(float3 c) {
	return lab2rgb(lch2lab(c));
}

