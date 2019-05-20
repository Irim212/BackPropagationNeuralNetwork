using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackpropagationNeuralNetwork
{
	internal class Layer
	{
		private readonly List<Neuron> neuronsList;

		internal Layer(int neuronsCount)
		{
			neuronsList = new List<Neuron>(neuronsCount);

			for (int i = 0; i < neuronsCount; i++)
			{
				neuronsList.Add(new Neuron());
			}
		}

		internal Layer(int neuronsCount, Layer leftSideLayer)
		{
			neuronsList = new List<Neuron>(neuronsCount);

			for (int i = 0; i < neuronsCount; i++)
			{
				neuronsList.Add(new Neuron(leftSideLayer.neuronsList));
			}
		}

		internal List<Neuron> getNeurons() => neuronsList;

		internal void sumWeights() => neuronsList.ForEach(x => x.sumWeights());

		internal void activate() => neuronsList.ForEach(x => x.activate());
	}
}
