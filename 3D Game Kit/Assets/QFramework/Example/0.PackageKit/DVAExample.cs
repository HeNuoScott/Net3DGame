using System;
using QF.DVA;
using UnityEngine;
using QFramework;
using Unidux;
using UniRx;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace QFramework.Example
{
    public partial class DVAExample : ViewController
    {
        void Start()
        {
            // Code Here

            CounterModel.Subject
                .StartWith(CounterModel.State)
                .Subscribe(state => { Number.text = state.Count.ToString(); });


            BtnAdd.onClick.AddListener(() => { CounterModel.Dispatch("increase"); });

            BtnSub.onClick.AddListener(() => { CounterModel.Dispatch("decrease"); });
        }
    }


    [Serializable]
    public class CounterState : DvaState
    {
        public int Count;
    }


    public class CounterModel : DvaModel<CounterModel, CounterState>
    {
        public override CounterState Reduce(CounterState state, DvaAction action)
        {
            switch (action.Type)
            {
                case "increase":
                    state.Count++;
                    break;
                case "decrease":
                    state.Count--;
                    break;
            }

            return state;
        }

        protected override string Namespace
        {
            get { return "count"; }
        }

        protected override CounterState InitialState
        {
            get { return new CounterState(); }
        }
    }
}