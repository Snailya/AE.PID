using AE.PID.Models.BOM;
using AE.PID.ViewModels;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace AE.PID.ViewModels
{
    public class ElementViewModel : ViewModelBase, IDisposable
    {
        private readonly IDisposable _cleanUp;
        private readonly ReadOnlyObservableCollection<ElementViewModel> _inferiors;
        private bool _isSelected;

        public ElementViewModel(Node<Element, int> node, ElementViewModel? parent = null)
        {
            Id = node.Key;
            Depth = node.Depth;
            Parent = parent;
            ParentId = node.Item.ParentId;
            Source = node.Item;

            Name = node.Item.Name;
            ProcessZone = node.Item.ProcessZone;
            FunctionalGroup = node.Item.FunctionalGroup;
            FunctionalElement = node.Item.Type == ElementType.Attached && node.Parent.HasValue
                ? $"{node.Parent.Value.Item.FunctionalElement}-{node.Item.FunctionalElement}"
                :node.Item.FunctionalElement;
            MaterialNo = node.Item.MaterialNo;
            Count = node.Item.Count;

            //Wrap loader for the nested view model inside a lazy so we can control when it is invoked
            var childrenLoader = node.Children.Connect()
                .Transform(e => new ElementViewModel(e, this))
                .Bind(out _inferiors)
                .DisposeMany()
                .Subscribe();

            _cleanUp = Disposable.Create(() => { childrenLoader.Dispose(); });
        }

        /// <summary>
        /// The id of the item
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The level of the item
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// The parent id of the item, used to reconstruct for tree
        /// </summary>
        public int ParentId { get; }

        /// <summary>
        /// The origin data
        /// </summary>
        public Element Source { get; }

        /// <summary>
        /// The parent of the item.
        /// </summary>
        public Optional<ElementViewModel> Parent { get; }
        
        /// <summary>
        /// The children of the item
        /// </summary>
        public ReadOnlyObservableCollection<ElementViewModel> Inferiors => _inferiors;

        /// <summary>
        /// The name displayed in View
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The process zone displayed in View
        /// </summary>
        public string ProcessZone { get; set; }

        /// <summary>
        /// The functional group displayed in View
        /// </summary>
        public string FunctionalGroup { get; set; }

        /// <summary>
        /// The functional element displayed in View
        /// </summary>
        public string FunctionalElement { get; set; }

        /// <summary>
        /// The material no displayed in View
        /// </summary>
        public string MaterialNo { get; set; }

        /// <summary>
        /// The count of item in View
        /// </summary>
        public double Count { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}

public class ElementViewModelComparer : IComparer<ElementViewModel>
{
    public int Compare(ElementViewModel x, ElementViewModel y)
    {
        return x.Source.CompareTo(y.Source);
    }
}