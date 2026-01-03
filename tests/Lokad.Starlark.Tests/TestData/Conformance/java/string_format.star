# Subset of test_suite/testdata/java string_format coverage.

assert_eq("abc".format(), "abc")
assert_eq("x{key}x".format(key=2), "x2x")
assert_eq("{{}}".format(), "{}")
assert_eq("{test} and {}".format(2, test=1), "1 and 2")
---
"{}".format() ### (Not enough arguments for format)
