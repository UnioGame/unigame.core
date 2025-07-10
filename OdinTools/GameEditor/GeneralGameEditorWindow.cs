namespace UniModules.GameEditor
{
    using System.Collections.Generic;
    using System.Linq;
    using Sirenix.OdinInspector.Editor;
    using Categories;
    using global::UniGame.Runtime.DataFlow;
    using global::UniGame.Core.Editor;

    public class GeneralGameEditorWindow<TConfiguration> : OdinMenuEditorWindow
        where TConfiguration : BaseEditorConfiguration
    {
        #region private data

        private TConfiguration _configuration;
        private HashSet<OdinMenuItem> _selectedItems = new();
        private List<IGameEditorCategory> _categories = new();
        private LifeTime _lifeTime = new();
        
        #endregion

        public TConfiguration Configuration
        {
            get
            {
                if(_configuration!= null)
                    return _configuration;
                _configuration = ValueTypeCache.LoadAsset<TConfiguration>();
                return _configuration;
            }
        }

        private void Rebuild()
        {
            ForceMenuTreeRebuild();
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            
            _lifeTime?.Terminate();
            _lifeTime = new LifeTime();
            
            var configuration = Configuration;
            configuration.UpdateAction -= Rebuild;
            configuration.UpdateAction += Rebuild;

            Rebuild();
            Rebuild();
        }
        
        protected override OdinMenuTree BuildMenuTree()
        {
            var configuration = Configuration;
            
            _categories = new List<IGameEditorCategory>(configuration.categories);
            
            var tree = new OdinMenuTree(false)
            {
                Config = {
                    DrawSearchToolbar = true
                }
            };
            
            tree.DefaultMenuStyle.IconSize             = configuration.iconSize;
            tree.DefaultMenuStyle.IconOffset           = configuration.iconOffset;
            tree.DefaultMenuStyle.NotSelectedIconAlpha = configuration.notSelectedIconAlpha;
            tree.DefaultMenuStyle.IconPadding          = configuration.iconPadding;

            // foreach (var category in _configuration.editorGroups)
            // {
            //     if(string.IsNullOrEmpty(category.Name))continue;
            //     
            //     tree.Add(category.Name,null,category.Icon);
            // }

            foreach (var editorCategory in _categories)
            {
                AddEditorCategory(editorCategory,editorCategory.Category,tree);
            }
            
            tree.Add(configuration.Category,configuration,configuration.Icon);
            
            var firstCategory = _categories.FirstOrDefault();
            if (firstCategory != null)
                TrySelectMenuItemWithObject(firstCategory);

            return tree;
        }

        protected void OnSelectionChange()
        {
            _selectedItems.Clear();
            foreach (var selectedItem in MenuTree.Selection)
            {
                _selectedItems.Add(selectedItem);
                if(selectedItem.Value is not IGameEditorView view)
                    continue;
                if(view.Active) continue;
                view.Focus();
            }

            foreach (var menuItem in MenuTree.MenuItems)
            {
                if(menuItem.Value is not IGameEditorView view)
                    continue;
                if(!_selectedItems.Contains(menuItem) && view.Active)
                    view.FocusLost();
            }
        }

        private void AddEditorCategory(IGameEditorCategory editorCategory,string categoryPath,OdinMenuTree tree )
        {
            if (editorCategory is not {Enabled: true}) 
                return;
                
            editorCategory.SetupConfiguration(Configuration);
                
            var category = editorCategory.UpdateCategory();
            var viewer = category.CreateDrawer();
                
            if(viewer == null) return;

            var path = GetFullPath(editorCategory, categoryPath);
            
            tree.Add(path,viewer,editorCategory.Icon);

            if (category is not IGameEditorCategoryList categoryList) 
                return;
            
            foreach (var item in categoryList.Categories)
            {
                AddEditorCategory(item, path,tree);
            }
        }
        
        private string GetFullPath(IGameEditorCategory category) => $"{category.Category}/{category.Name}";
        
        private string GetFullPath(IGameEditorCategory category,string categoryName) => $"{categoryName}/{category.Name}";
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Configuration.UpdateAction -= Rebuild;
            _lifeTime.Terminate();
        }
    }
}