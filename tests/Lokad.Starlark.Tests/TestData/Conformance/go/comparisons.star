# Subset of go suite to cover list/tuple ordering comparisons.

assert_([1] < [1, 1])
assert_([1, 2] < [2])
assert_(["a", "b"] > ["a"])
assert_(("a", "b") >= ("a",))
