# Subset of test_suite/testdata/go int coverage.

assert_eq(5 + 7, 12)
assert_eq(5 - 7, -2)
assert_eq(5 * 7, 35)
assert_eq(100 // 7, 14)
assert_eq(100 % 7, 2)
---
x = 1
x += 1
x -= 3
x *= 2
x //= 3
x %= 2
assert_eq(x, 1)
---
assert_(1 < 2)
assert_(2 > 1)
assert_(2 >= 2)
assert_(1 <= 1)
---
assert_(1 == 1.0)
assert_(1.0 == 1)
assert_(1 < 2.0)
assert_(2.0 > 1)
assert_(1.0 <= 1)
assert_(2.0 >= 2)
---
assert_eq(int("123"), 123)
assert_eq(int("-123"), -123)
---
assert_eq(+5, 5)
assert_eq(~1, -2)
assert_eq(5 & 3, 1)
assert_eq(5 | 2, 7)
assert_eq(5 ^ 6, 3)
assert_eq(1 << 3, 8)
assert_eq(8 >> 2, 2)
1 << -1 ### (shift count)
---
x = 5
x |= 2
x &= 7
x ^= 3
x <<= 1
x >>= 2
assert_eq(x, 2)
