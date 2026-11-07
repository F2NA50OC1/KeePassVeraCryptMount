# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 2.0.x | yes |
| 1.0.7 and older (original project) | no — does not load in KeePass 2.62, see [CHANGELOG.md](CHANGELOG.md) |

## Reporting a vulnerability

Please report security problems privately, through this repository's **Security → Report a
vulnerability** page (GitHub private vulnerability reporting), not in a public issue.

Describe what you did, what happened and which versions of KeePass, VeraCrypt, Windows / Linux
and the plugin you used. **Never include real passwords, key files or database files** — a test
database and a test volume are enough to show a problem.

Examples of what is in scope:

* the password (or any part of it) typed into a window other than VeraCrypt's password window;
* a value stored in an entry that adds options to VeraCrypt's command line, or reaches it unchecked;
* a secret ending up on a command line, in a log or in KeePass' configuration file;
* KeePass settings changed by the plugin that are not restored;
* a release file that differs from what `tools/build.sh` produces from the tagged source (for the
  `.plgx`: apart from the random file ID that KeePass gives every package it creates).

Out of scope here (please report to the right project): problems in KeePass itself
(https://keepass.info) or VeraCrypt itself (https://veracrypt.io), and attacks that need
malware already running under your user account (it can read keystrokes and KeePass' memory).

## How the plugin protects the password

See "How Auto-Type is kept safe" in [README.md](../README.md), and the full review with every finding
and fix in [SECURITY-REVIEW.md](SECURITY-REVIEW.md). For the highest security, uncheck
"Auto-Type" in the entry's mount settings and type the password into VeraCrypt yourself.
