﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using TestUtilities.SharedProject;
using TestUtilities.UI;
using Keyboard = TestUtilities.UI.Keyboard;
using Mouse = TestUtilities.UI.Mouse;

namespace Microsoft.VisualStudioTools.SharedProjectTests {
    [TestClass]
    public class NewDragDropCopyCutPaste : SharedProjectTest {
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveToMissingFolderKeyboard() {
            MoveToMissingFolder(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveToMissingFolderMouse() {
            MoveToMissingFolder(MoveByMouse);
        }

        private void MoveToMissingFolder(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveToMissingFolder",
                    projectType,
                    PropertyGroup(
                        Property("ProjectView", "ShowAllFiles")
                    ),
                    ItemGroup(
                        Folder("Fob", isExcluded: false, isMissing: true),
                        Compile("codefile", isExcluded: false)
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveToMissingFolder", "codefile" + projectType.CodeExtension),
                        solution.FindItem("MoveToMissingFolder", "Fob")
                    );

                    solution.AssertFileDoesntExist("MoveToMissingFolder", "codefile" + projectType.CodeExtension);
                    solution.AssertFileExists("MoveToMissingFolder", "Fob", "codefile" + projectType.CodeExtension);
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveExcludedFolderKeyboard() {
            MoveExcludedFolder(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveExcludedFolderMouse() {
            MoveExcludedFolder(MoveByMouse);
        }

        private void MoveExcludedFolder(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveExcludedFolder",
                    projectType,
                    PropertyGroup(
                        Property("ProjectView", "ShowAllFiles")
                    ),
                    ItemGroup(
                        Folder("Fob", isExcluded: true),
                        Folder("Fob\\Oar", isExcluded: true),
                        Folder("Baz", isExcluded: true)
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveExcludedFolder", "Baz"),
                        solution.FindItem("MoveExcludedFolder", "Fob")
                    );

                    solution.AssertFolderDoesntExist("MoveExcludedFolder", "Fob");
                    solution.AssertFolderExists("MoveExcludedFolder", "Baz", "Fob");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveExcludedItemToFolderKeyboard() {
            MoveExcludedItemToFolder(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveExcludedItemToFolderMouse() {
            MoveExcludedItemToFolder(MoveByMouse);
        }

        private void MoveExcludedItemToFolder(MoveDelegate mover) {

            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveExcludedItemToFolder",
                    projectType,
                    PropertyGroup(
                        Property("ProjectView", "ShowAllFiles")
                    ),
                    ItemGroup(
                        Folder("Folder"),
                        Compile("codefile", isExcluded: true)
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveExcludedItemToFolder", "Folder"),
                        solution.FindItem("MoveExcludedItemToFolder", "codefile" + projectType.CodeExtension)
                    );

                    solution.AssertFileDoesntExist("MoveExcludedItemToFolder", "codefile" + projectType.CodeExtension);
                    solution.AssertFileExists("MoveExcludedItemToFolder", "Folder", "codefile" + projectType.CodeExtension);
                    Assert.IsTrue(solution.Project.GetIsFolderExpanded("Folder"));

                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameSkipMoveKeyboard() {
            MoveDuplicateFileNameSkipMove(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameSkipMoveMouse() {
            MoveDuplicateFileNameSkipMove(MoveByMouse);
        }

        /// <summary>
        /// Move item within the project from one location to where it already exists, skipping the move.
        /// </summary>
        private void MoveDuplicateFileNameSkipMove(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveDuplicateFileName",
                    projectType,
                    ItemGroup(
                        Folder("Folder"),
                        Content("textfile.txt", "root"),
                        Content("Folder\\textfile.txt", "Folder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveDuplicateFileName", "Folder"),
                        solution.FindItem("MoveDuplicateFileName", "textfile.txt")
                    );

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.No();
                    }

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("root", "MoveDuplicateFileName", "textfile.txt");
                    solution.AssertFileExistsWithContent("Folder", "MoveDuplicateFileName", "Folder", "textfile.txt");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNamesSkipOneKeyboard() {
            MoveDuplicateFileNamesSkipOne(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNamesSkipOneMouse() {
            MoveDuplicateFileNamesSkipOne(MoveByMouse);
        }

        /// <summary>
        /// Cut 2 items, paste where they exist, skip pasting the 1st one but paste the 2nd.
        /// 
        /// The 1st item shouldn't be removed from the parent hierarchy, the 2nd should, and only the 2nd item should be overwritten.
        /// </summary>
        private void MoveDuplicateFileNamesSkipOne(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveDuplicateFileName",
                    projectType,
                    ItemGroup(
                        Folder("Folder"),
                        Content("textfile1.txt", "root1"),
                        Content("textfile2.txt", "root2"),
                        Content("Folder\\textfile1.txt", "Folder1"),
                        Content("Folder\\textfile2.txt", "Folder2")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveDuplicateFileName", "Folder"),
                        solution.FindItem("MoveDuplicateFileName", "textfile1.txt"),
                        solution.FindItem("MoveDuplicateFileName", "textfile2.txt")
                    );

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.No();
                    }

                    System.Threading.Thread.Sleep(1000);

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.Yes();
                    }

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("root1", "MoveDuplicateFileName", "textfile1.txt");
                    solution.AssertFileDoesntExist("MoveDuplicateFileName", "textfile2.txt");
                    solution.AssertFileExistsWithContent("Folder1", "MoveDuplicateFileName", "Folder", "textfile1.txt");
                    solution.AssertFileExistsWithContent("root2", "MoveDuplicateFileName", "Folder", "textfile2.txt");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VsTestHost")]
        public void MoveDuplicateFileNamesFoldersSkipOneKeyboard() {
            MoveDuplicateFileNamesFoldersSkipOne(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VsTestHost")]
        public void MoveDuplicateFileNamesFoldersSkipOneMouse() {
            MoveDuplicateFileNamesFoldersSkipOne(MoveByMouse);
        }

        /// <summary>
        /// Cut 2 items, paste where they exist, skip pasting the 1st one but paste the 2nd.
        /// 
        /// The 1st item shouldn't be removed from the parent hierarchy, the 2nd should, and only the 2nd item should be overwritten.
        /// </summary>
        private void MoveDuplicateFileNamesFoldersSkipOne(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("MoveDuplicateFileName",
                    projectType,
                    ItemGroup(
                        Folder("Source"),
                        Content("Source\\textfile1.txt", "source1"),
                        Content("Source\\textfile2.txt", "source2"),
                        
                        Folder("Target"),
                        Content("Target\\textfile1.txt", "target1"),
                        Content("Target\\textfile2.txt", "target2")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.FindItem("MoveDuplicateFileName", "Target"),
                        solution.FindItem("MoveDuplicateFileName", "Source", "textfile1.txt"),
                        solution.FindItem("MoveDuplicateFileName", "Source", "textfile2.txt")
                    );

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.No();
                    }

                    System.Threading.Thread.Sleep(1000);

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.Yes();
                    }

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("source1", "MoveDuplicateFileName", "Source", "textfile1.txt");
                    solution.AssertFileDoesntExist("MoveDuplicateFileName", "textfile2.txt");
                    solution.AssertFileExistsWithContent("target1", "MoveDuplicateFileName", "Target", "textfile1.txt");
                    solution.AssertFileExistsWithContent("source2", "MoveDuplicateFileName", "Target", "textfile2.txt");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNamesCrossProjectSkipOneKeyboard() {
            MoveDuplicateFileNamesCrossProjectSkipOne(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNamesCrossProjectSkipOneMouse() {
            MoveDuplicateFileNamesCrossProjectSkipOne(MoveByMouse);
        }

        /// <summary>
        /// Cut 2 items, paste where they exist, skip pasting the 1st one but paste the 2nd.
        /// 
        /// The 1st item shouldn't be removed from the parent hierarchy, the 2nd should, and only the 2nd item should be overwritten.
        /// </summary>
        private void MoveDuplicateFileNamesCrossProjectSkipOne(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projectDefs = new[] {
                    new ProjectDefinition("MoveDuplicateFileName",
                        projectType,
                        ItemGroup(
                            Content("textfile1.txt", "textfile1 - lang"),
                            Content("textfile2.txt", "textfile2 - lang")
                        )
                    ),
                    new ProjectDefinition("MoveDuplicateFileName2",
                        projectType,
                        ItemGroup(
                            Folder("Folder"),
                            Content("textfile1.txt", "textfile1 - 2"),
                            Content("textfile2.txt", "textfile2 - 2")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("MoveDuplicateFileName", projectDefs).ToVs()) {
                    var item1 = solution.FindItem("MoveDuplicateFileName", "textfile1.txt");
                    var item2 = solution.FindItem("MoveDuplicateFileName", "textfile2.txt");
                    mover(
                        solution.FindItem("MoveDuplicateFileName2"),
                        item1,
                        item2
                    );

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.No();
                    }

                    System.Threading.Thread.Sleep(1000);

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.Yes();
                    }

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("textfile1 - lang", "MoveDuplicateFileName", "textfile1.txt");
                    solution.AssertFileExistsWithContent("textfile2 - lang", "MoveDuplicateFileName", "textfile2.txt");
                    solution.AssertFileExistsWithContent("textfile1 - 2", "MoveDuplicateFileName2", "textfile1.txt");
                    solution.AssertFileExistsWithContent("textfile2 - lang", "MoveDuplicateFileName2", "textfile2.txt");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameCrossProjectSkipMoveKeyboard() {
            MoveDuplicateFileNameCrossProjectSkipMove(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameCrossProjectSkipMoveMouse() {
            MoveDuplicateFileNameCrossProjectSkipMove(MoveByMouse);
        }

        /// <summary>
        /// Move item to where an item by that name exists across 2 projects of the same type.
        /// 
        /// https://pytools.codeplex.com/workitem/1967
        /// </summary>
        private void MoveDuplicateFileNameCrossProjectSkipMove(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projectDefs = new[] {
                    new ProjectDefinition("MoveDuplicateFileName1",
                        projectType,
                        ItemGroup(
                            Content("textfile.txt", "MoveDuplicateFileName1")
                        )
                    ),
                    new ProjectDefinition("MoveDuplicateFileName2",
                        projectType,
                        ItemGroup(
                            Folder("Folder"),
                            Content("textfile.txt", "MoveDuplicateFileName2")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("MoveDuplicateFileName", projectDefs).ToVs()) {
                    mover(
                        solution.FindItem("MoveDuplicateFileName2"),
                        solution.FindItem("MoveDuplicateFileName1", "textfile.txt")
                    );

                    using (var dialog = OverwriteFileDialog.Wait(solution.App)) {
                        dialog.No();
                    }

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("MoveDuplicateFileName1", "MoveDuplicateFileName1", "textfile.txt");
                    solution.AssertFileExistsWithContent("MoveDuplicateFileName2", "MoveDuplicateFileName2", "textfile.txt");
                }

            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameCrossProjectCSharpSkipMoveKeyboard() {
            MoveDuplicateFileNameCrossProjectCSharpSkipMove(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveDuplicateFileNameCrossProjectCSharpSkipMoveMouse() {
            MoveDuplicateFileNameCrossProjectCSharpSkipMove(MoveByMouse);
        }

        /// <summary>
        /// Move item to where item exists across project types.
        /// </summary>
        private void MoveDuplicateFileNameCrossProjectCSharpSkipMove(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projectDefs = new[] {
                    new ProjectDefinition("MoveDuplicateFileName1",
                        projectType,
                        ItemGroup(
                            Content("textfile.txt", "MoveDuplicateFileName1")
                        )
                    ),
                    new ProjectDefinition("MoveDuplicateFileNameCS",
                        ProjectType.CSharp,
                        ItemGroup(
                            Folder("Folder"),
                            Content("textfile.txt", "MoveDuplicateFileNameCS")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("MoveDuplicateFileName", projectDefs).ToVs()) {
                    mover(
                        solution.FindItem("MoveDuplicateFileNameCS"),
                        solution.FindItem("MoveDuplicateFileName1", "textfile.txt")
                    );

                    // say no to replacing in the C# project system
                    solution.App.WaitForDialog();
                    Keyboard.Type(Key.N);

                    solution.App.WaitForDialogDismissed();

                    solution.AssertFileExistsWithContent("MoveDuplicateFileName1", "MoveDuplicateFileName1", "textfile.txt");
                    solution.AssertFileExistsWithContent("MoveDuplicateFileNameCS", "MoveDuplicateFileNameCS", "textfile.txt");
                }

            }
        }

        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveFileFromFolderToLinkedFolderKeyboard() {
            MoveFileFromFolderToLinkedFolder(MoveByKeyboard);
        }

        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("VSTestHost")]
        public void MoveFileFromFolderToLinkedFolderMouse() {
            MoveFileFromFolderToLinkedFolder(MoveByMouse);
        }

        /// <summary>
        /// Move item to a folder that has a symbolic link.  Verify we cannot move 
        /// ourselves to ourselves and that moves are reflected in both the folder and its symbolic link.
        /// NOTE: Because of symbolic link creation, this test must be run as administrator.
        /// </summary>
        private void MoveFileFromFolderToLinkedFolder(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projectDefs = new[] {
                    new ProjectDefinition("MoveLinkedFolder",
                        projectType,
                        ItemGroup(
                            Content("textfile.txt", "text file contents"),
                            Folder("Folder"),
                            Content("Folder\\FileInFolder.txt", "File inside of linked folder..."),
                            SymbolicLink("FolderLink", "Folder")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("MoveLinkedFolder", projectDefs).ToVs()) {
                    mover(
                        solution.FindItem("MoveLinkedFolder", "FolderLink"),
                        solution.FindItem("MoveLinkedFolder", "Folder", "FileInFolder.txt")
                    );

                    // Say okay to the error that pops up since we can't move to ourselves.
                    solution.App.WaitForDialog();
                    Keyboard.Type(Key.Enter);

                    solution.App.WaitForDialogDismissed();

                    // Verify that after the dialog our files are still present.
                    solution.AssertFileExists("MoveLinkedFolder", "FolderLink", "FileInFolder.txt");
                    solution.AssertFileExists("MoveLinkedFolder", "Folder", "FileInFolder.txt");

                    // Now move the text file in the root.  Expect it to move and be in both.
                    mover(
                        solution.FindItem("MoveLinkedFolder", "FolderLink"),
                        solution.FindItem("MoveLinkedFolder", "textfile.txt")
                    );

                    solution.AssertFileExists("MoveLinkedFolder", "FolderLink", "textfile.txt");
                    solution.AssertFileExists("MoveLinkedFolder", "Folder", "textfile.txt");
                }
            }
        }

        /// <summary>
        /// Selects the provided items with the mouse preparing for a drag and drop
        /// </summary>
        /// <param name="source"></param>
        private static void SelectItemsForDragAndDrop(AutomationElement[] source) {
            AutomationWrapper.Select(source.First());
            for (int i = 1; i < source.Length; i++) {
                AutomationWrapper.AddToSelection(source[i]);
            }

            Mouse.MoveTo(source.Last().GetClickablePoint());
            Mouse.Down(MouseButton.Left);
        }

        /// <summary>
        /// Moves one or more items in solution explorer to the destination using the mouse.
        /// </summary>
        private static void MoveByMouse(AutomationElement destination, params AutomationElement[] source) {
            SelectItemsForDragAndDrop(source);

            try {
                try {
                    Keyboard.Press(Key.LeftShift);
                    Mouse.MoveTo(destination.GetClickablePoint());
                } finally {
                    Mouse.Up(MouseButton.Left);
                }
            } finally {
                Keyboard.Release(Key.LeftShift);
            }
        }

        /// <summary>
        /// Moves one or more items in solution explorer using the keyboard to cut and paste.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        private static void MoveByKeyboard(AutomationElement destination, params AutomationElement[] source) {
            AutomationWrapper.Select(source.First());
            for (int i = 1; i < source.Length; i++) {
                AutomationWrapper.AddToSelection(source[i]);
            }

            Keyboard.ControlX();

            AutomationWrapper.Select(destination);
            Keyboard.ControlV();
        }

        private delegate void MoveDelegate(AutomationElement destination, params AutomationElement[] source);
    }
}
