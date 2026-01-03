# Subset of spec bytes coverage.

b = b"ABC"
assert_eq(type(b), "bytes")
assert_eq(len(b), 3)
assert_eq(b[0], 65)
assert_eq(b[1:3], b"BC")
assert_eq(b + b"DE", b"ABCDE")
assert_eq(b * 2, b"ABCABC")
assert_(65 in b)
assert_(b"BC" in b)
assert_(not (b"CB" in b))
---
assert_eq(bytes("ABC"), b"ABC")
assert_eq(bytes([65, 66, 67]), b"ABC")
bytes(256) ### (string, bytes, or iterable)
---
assert_eq(list(b"ABC".elems()), [65, 66, 67])
