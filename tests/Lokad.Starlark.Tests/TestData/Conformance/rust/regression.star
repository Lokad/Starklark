# Subset of test_suite/testdata/rust regression coverage.

"abc" * True ### (Operator '.*' not supported|unsupported|unknown binary op)
---
assert_eq(3 * "abc", "abcabcabc")
