namespace AccessMind.SharpLouis;

// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
// Copyright © 2024 AccessMind LLC.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

/// <summary>
/// Thrown when a LibLouis operation fails: a table that cannot be compiled, or a translation the
/// native library rejects. Missing prerequisites (the native DLL, the tables folder, or a requested
/// table file) surface as the corresponding <see cref="System.DllNotFoundException"/>,
/// <see cref="System.IO.DirectoryNotFoundException"/> or <see cref="System.IO.FileNotFoundException"/>.
/// </summary>
public sealed class LouisException: Exception {
    public LouisException() {
    }

    public LouisException(string message) : base(message) {
    }

    public LouisException(string message, Exception innerException) : base(message, innerException) {
    }
}
