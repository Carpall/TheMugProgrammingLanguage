import std.err.Err;

std.text
{
	# Member
	type Stringer
	{
		raw: [chr],
		len: u32,
		seg: u32
	}
	func Stringer(): Stringer
	{
		return new Stringer {
			raw: new [chr, 1000] { },
			len: 0,
			seg: 1000
		};
	}

	# Private
	func match_overflow(self: Stringer): bit
	{
		return self.len == self.seg;
	}
	func match_overflow(self: Stringer, count: u32): bit
	{
		return self.len+count >= self.seg;
	}
	func expand(self: Stringer)
	{
		var swap_buf: [chr] = self.raw;
		self.seg = self.seg * 2;
		self.raw = new [chr, self.seg] { };
		for i: u32 to self.len
		{
			self.raw[i] = swap_buf[i];
		}
	}
	func unsafe_append(self: Stringer, value: chr)
	{
		self.raw[self.len] = value;
		self.len = self.len+1;
	}

	# Public
	func append(self: Stringer, value: chr)
	{
		if self.match_overflow()
		{
			self.expand();
		}
		self.unsafe_append(value);
	}
	func append(self: Stringer, value: str)
	{
		if self.match_overflow(value.len())
		{
			self.expand();
		}
		for i: u32 to value.len()
		{
			self.unsafe_append(value[i]);
		}
	}
	func pop(self: Stringer): chr
	{
		self.len = self.len-1;
		return self.raw[self.len-1];
	}
	func pop_top(self: Stringer)
	{
		self.len = self.len-1;
	}
	func get(self: Stringer, index: u32): Err[chr]
	{
		if self.len > index
		{
			return Err[chr](); # add generic functions
		}
	}

	func test_stringer()
	{
		
	}
}