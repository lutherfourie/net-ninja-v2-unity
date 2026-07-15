#!/usr/bin/env bash
BRANCH=$1; PATH_WT=${2:-../$1}
git worktree add -b "$BRANCH" "$PATH_WT"
echo "Worktree $PATH_WT — open Unity there so Library is per-worktree."
