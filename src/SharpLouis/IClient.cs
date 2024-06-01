namespace SharpLouis;

/// <summary>
/// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
/// Copyright © 2024 AccessMind LLC.
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing,
/// software distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
//////
///All callers of the <c>Wrapper</c> class must implement this interface to provide logging/>
/// </summary>
public interface IClient {
    /// <summary>
    /// Called by the wrapper for normal logging
    /// </summary>
    /// <param name="message">Message to be written to the log</param>
    void OnWrapperLog(string message);

        /// <summary>
    /// Called by the wrapper for logging of messages received from LibLouis native code through the Callback mechanism
    /// </summary>
    /// <param name="message">Message to be written to the log</param>
    void OnLibLouisLog(string message);
}
