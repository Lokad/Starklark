# Subset of test_suite/testdata/go builtins coverage.

assert_eq(len([1, 2, 3]), 3)
assert_eq(len("abc"), 3)
---
assert_eq(bool(), False)
assert_eq(float(), 0.0)
---
assert_eq(list(range(3)), [0, 1, 2])
assert_eq(list(range(1, 4)), [1, 2, 3])
assert_eq(list(range(1, 6, 2)), [1, 3, 5])
---
assert_eq(sorted([3, 1, 2]), [1, 2, 3])
assert_eq(reversed([1, 2, 3]), [3, 2, 1])
---
assert_eq(min([3, 1, 2]), 1)
assert_eq(max([3, 1, 2]), 3)
---
assert_eq(enumerate(["a", "b"]), [(0, "a"), (1, "b")])
assert_eq(zip([1, 2], ["a", "b"]), [(1, "a"), (2, "b")])
---
assert_(any([0, "", 1]))
assert_(not any([0, "", []]))
assert_(all([1, "x", [0]]))
assert_(not all([1, "", 2]))
