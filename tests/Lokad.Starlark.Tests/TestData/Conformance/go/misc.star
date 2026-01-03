# Subset of test_suite/testdata/go misc coverage.

assert_eq(type(1), "int")
assert_eq(type("a"), "string")
assert_eq(type([1]), "list")
assert_eq(type((1,)), "tuple")
---
assert_(hasattr("abc", "upper"))
assert_(not hasattr("abc", "nope"))
assert_eq(getattr("abc", "upper")(), "ABC")
