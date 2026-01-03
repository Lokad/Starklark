# Subset of test_suite/testdata/rust string coverage.

assert_eq("ab1cd2ef", "ab%scd%sef" % [1, 2])
---
"" % 0 ### (Not all arguments converted|Not enough arguments|not iterable)
---
"" % (0,) ### (Too many arguments|not all arguments)
