# Subset of test_suite/testdata/go tuple coverage.

assert_eq((1,), (1,))
assert_eq((1, 2), (1, 2))
assert_ne((1, 2), (1, 3))
assert_((False,))
---
assert_eq(("a", "b")[0], "a")
assert_eq(("a", "b")[1], "b")
---
assert_(not tuple())
assert_eq(tuple(), tuple([]))
assert_eq(tuple([1]), (1,))
---
abc = tuple(["a", "b", "c"])
assert_eq(abc * 2, ("a", "b", "c", "a", "b", "c"))
assert_eq(2 * abc, ("a", "b", "c", "a", "b", "c"))
