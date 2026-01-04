# Subset of test_suite/testdata/rust string coverage.

assert_eq("ab1cd2ef", "ab%scd%sef" % [1, 2])
assert_eq("ab[1]cd", "ab%scd" % [1])
---
"" % 0 ### (Not all arguments converted|Not enough arguments|not iterable)
---
"" % (0,) ### (Too many arguments|not all arguments)
