// Share cards on WebGL (Daily play structure, Pass 8c): a browser has no share sheet we can rely on, so SHARE downloads the card
// PNG and the player posts it themselves. Called from WebGlShareDownload.cs. The download starts from a click on a temporary
// anchor with a blob URL, which the browser allows for a download without any permission prompt.
mergeInto(LibraryManager.library, {
	// Returns 1 when the download was started, 0 when anything failed.
	Opoint8182_DownloadPng: function (dataPointer, length, fileNamePointer) {
		try {
			// slice() copies out of the WebAssembly heap: the heap can move or be reused before the blob is read.
			var bytes = HEAPU8.slice(dataPointer, dataPointer + length);
			var blob = new Blob([bytes], { type: 'image/png' });
			var url = URL.createObjectURL(blob);

			var anchor = document.createElement('a');
			anchor.href = url;
			anchor.download = UTF8ToString(fileNamePointer);
			anchor.style.display = 'none';
			document.body.appendChild(anchor);
			anchor.click();

			setTimeout(function () {
				document.body.removeChild(anchor);
				URL.revokeObjectURL(url);
			}, 2000);
			return 1;
		} catch (error) {
			console.warn('[Share] download failed: ' + error);
			return 0;
		}
	}
});
