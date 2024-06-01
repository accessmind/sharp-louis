using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using SharpLouis.BrailleTranslationTable;

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
///Translation tables collection.
/// Workks with the tables.json file provided in this library as part of LibLouis and actually being a result of tables processing made by the LLJT console utility
/// <seealso href="https://github.com/accessmind/liblouis-jsonify-tables"/>
/// </summary>
public class TableCollection: ICollection<TranslationTable> {
    private const string TablesJson = @"LibLouis\tables.json";
    private List<TranslationTable> tables = new List<TranslationTable>();

    public int Count { get { return tables.Count; } }

    public bool IsReadOnly { get;}

    public TableCollection PopulateFromJson() {
        using var file = File.OpenRead(TablesJson);
        this.tables = JsonSerializer.Deserialize<List<TranslationTable>>(file);
        return this;
    }

    public TableCollection FindByLanguage(string language) {
        this.tables = this.tables.FindAll(t => t.Language == language).ToList();
        return this;
    }

    public TableCollection FindLiterary() {
        this.tables = this.tables.FindAll(t => t.IsLiteraryBraille());
        return this;
    }

    public TranslationTable FindByFileName(string fileName) {
        return this.tables.Find(t => t.FileName == fileName);
    }

    public Dictionary<string, string> ListLanguages() {
        return (from table in this.tables.DistinctBy(t => t.Language)
                select (table.Language, new CultureInfo(table.Language.Split('-')[0]).EnglishName))
                .Distinct()
               .ToDictionary();
                                   }

        public void Add(TranslationTable item) => ((ICollection<TranslationTable>)tables).Add(item);
    public void Clear() => ((ICollection<TranslationTable>)tables).Clear();
    public bool Contains(TranslationTable item) => ((ICollection<TranslationTable>)tables).Contains(item);
    public void CopyTo(TranslationTable[] array, int arrayIndex) => ((ICollection<TranslationTable>)tables).CopyTo(array, arrayIndex);
    public bool Remove(TranslationTable item) => ((ICollection<TranslationTable>)tables).Remove(item);
    public IEnumerator<TranslationTable> GetEnumerator() => ((IEnumerable<TranslationTable>)tables).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)tables).GetEnumerator();
}
