# IL2CPP floating-point contract

Pin **fp-contract=off** on all IL2CPP player builds (Android arm64, WebGL, iOS) so the C++ backend
does not rewrite `a*b+c` into FMA. Without this, ARM can diverge from golden doubles.

GameCI / Player build scripts must inject the equivalent of:
`-ffp-contract=off` (clang) / `/fp:precise` style contract disable for the IL2CPP toolchain.

Do **not** enable Burst fastmath on Core (Core is managed, no Burst).
