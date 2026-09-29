# "Israel Auth" mitigation project

A community defense project against the "Israel Auth" malware campaign that targeted
Gorilla Tag modding installs (BepInEx plugins).

This repository hosts the tooling used to detect, sample, and eventually clean
affected machines.

## Purpose

The project exists to:

1. identify compromised machines in the community,
2. acquire reference samples of the campaign's artifacts,
3. document what the campaign did, for users, server admins, and AV vendors.

## Status: stage 001 - scanner

`001_scanner/` contains:

- `Program.cs` - source
- `israelauth-check.exe` - compiled checker

### What it does

A read-only sweep of the machine for known campaign indicators:

- Gorilla Tag installs (default paths, Steam registry, `libraryfolders.vdf`, manual args)
- infected plugin DLLs and patcher DLLs (resource/marker/hash signatures)
- the hidden `BepInEx/plugins/.graze/` payload directory
- the `PluginInjector.exe` daemon (live process args, known hash, renamed variants)
- persistence (`Run` keys) and execution traces (Prefetch, BepInEx logs)

Anything matched is packed into a single timestamped zip together with a `report.txt`
of the scan, which the user sends to the maintainers manually.

---

Nothing beyond the first stage is documented here yet.
