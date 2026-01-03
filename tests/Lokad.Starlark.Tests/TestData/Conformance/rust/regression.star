# Subset of test_suite/testdata/rust regression coverage.

"abc" * True ### (Operator '.*' not supported|unsupported)
---
assert_eq(3 * "abc", "abcabcabc")
