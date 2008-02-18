class Either<T, U> {
	T first;
	U second;
	bool bFirstOrSecond;
	Either(T x) {
		first = x;
		bFirstOrSecond = true;
	}
	Either(U y) {
		second = y;
		bFirstOrSecond = false;
	}
}