# Subset of test_suite/testdata/go bool coverage.

assert_(True)
assert_(not False)
assert_eq([bool(False), bool(1), bool(0), bool("hello"), bool("")], [False, True, False, True, False])
---
assert_(None == None)
assert_(None != False)
assert_eq(1 == 1, True)
assert_eq(1 == 2, False)
---
assert_eq(0 or "" or [] or 123, 123)
0 or "" or [] or 0 or 1 // 0 ### (Division by zero)
---
assert_eq(1 and "a" and [1] and 123, 123)
1 and "a" and [1] and 123 and 1 // 0 ### (Division by zero)
