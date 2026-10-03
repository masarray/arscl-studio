# M2A acceptance and manual smoke test

Automated gates run with `dotnet test ArSclStudio.sln --configuration Release` on Windows, Ubuntu and macOS. CI attaches TRX evidence and a self-contained Windows desktop build.

Verified implementation: `2f425acabada770dc89a5370314a0b8c54f32dc3`, [CI 37116099309](https://github.com/masarray/arscl-studio/actions/runs/37116099309), **50/50 tests passed on each OS**. The 100k-DAI edit measured 189.6–219.8 ms and approximately 36.3 MB allocated on these runners. See the handoff for per-platform values and the manual acceptance boundary.

## Automated coverage

- apply, old-value/revision preconditions, compound single commit;
- duplicate/unsupported targets, vendor namespace isolation, invalid XML characters and bounded values;
- rollback after an injected post-mutation validator failure;
- cancellation after staging and lifetime cancellation/drain;
- competing edits and Open versus edit stale publication;
- undo/redo, new-edit redo invalidation, no-op handling and saved-content dirty state;
- null versus empty description and preservation of vendor `desc` attributes;
- bounded history/journal and repeated attribute removal/recreation without registry growth;
- published old snapshot immutability and old snapshot collectability while undo remains available;
- no-edit/edited round trips, comments, PI, CDATA, prefixes, vendor content, attribute tabs/CR/LF;
- UTF-16 BOM input, UTF-8 output and declaration-free input;
- Save As, source/handle rebinding, explicit overwrite and file-role preservation;
- external-file conflict, stale/cancelled save, I/O failure and failed atomic move;
- verification rejection when vendor content differs;
- Desktop ViewModel edit/apply/undo/redo/save flow, stable selected IED, draft targeting and cancel;
- 100,000 DAI staging time/allocation, semantic index reuse and collapsed-tree laziness;
- all existing M1B security, reference, coalescing, leak and large-tree gates.

## Manual desktop smoke test

1. Run `ArSclStudio.Desktop.exe` from the extracted Windows artifact, or run Desktop with .NET 10 on another supported OS.
2. Open a real ICD/IID/CID/SCD. Select a standard SCL IED in Engineering or XML.
3. Choose **Edit description**, type a value and **Apply**. The dirty indicator and Changes row must update.
4. Check the same unqualified `desc` in XML. A vendor-prefixed `desc` must remain unchanged.
5. Undo, redo, then Save. Reopen using both ARSCL and a trusted vendor tool. Validate vendor import separately; M2A does not certify target compatibility.
6. Try Save As to a new path with the same extension. Confirm the original file was preserved.
7. Modify the input outside ARSCL, then try Save: ARSCL must reject the overwrite.
8. With an unapplied draft, navigate to another IED: the editor label must still identify its original target. Cancel must leave XML unchanged.
9. With committed unsaved changes, Open/Close must offer Save, Discard or Cancel. A failed save must keep the window/document open.
10. Open a malformed file while the active document is dirty: the active document and its history must survive the failed open.

Native file picker, close-dialog behavior, DPI/layout and actual vendor import require this human smoke test. Do not mark them visually verified solely from CI.

## Headless usage

```sh
dotnet run --project src/ArSclStudio.Cli -- set-description input.scd Relay_A "Feeder A" output.scd
```

This uses the same Engine transaction and verified save path. The IED name must resolve uniquely. An existing different output file is not overwritten by this CLI command.
