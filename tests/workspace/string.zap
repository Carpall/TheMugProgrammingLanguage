pub func strlen(text: str): i64;

pub type String { raw: [chr], len: i32, cap: i32 }

pub func String(): String {
	return new String { raw: new [chr, 1000] { }, cap: 1000 };
}

pub func String(text: str): String {
	const len = strlen(text) as i32;
	var allocation = new [chr, len] { };
	var i: i32;

	# copying the readonly string to the new heap allocation
	while i < len { allocation[i] = text[i]; }

	return new String { raw: allocation, len: len, cap: len + 1000 };
}

pub func String(capacity: i32) {
	return new String { raw: new [chr] { }, cap: capacity };
}

pub func (self: String) to_array(): [chr] {
	const len = self.len;
	var allocation = new [chr, len] { };
	var i: i32;

	while i < len { allocation[i] = self.raw[i]; }

	return allocation;
}

pub func (self: String) to_pointer(): [chr] {
	return self.raw;
}

pub func (this: *String) add(text: str) {
	const len = strlen(text);
	const cap = (*this).cap;
	
	if len > cap {
	}
}

pub func (this: *String) add(text: [chr]) {
}

pub func (this: *String) add(text: String) {
	
}