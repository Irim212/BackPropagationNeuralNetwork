
using System;

namespace BackpropagationNeuralNetwork
{
	public class NetworkBuilder
	{
		private NetworkConfiguration networkConfiguration;

		private NetworkBuilder()
		{
			networkConfiguration = new NetworkConfiguration()
			{
				InputLayerNeurons = 20,
				HiddenLayerNeurons = 4,
				OutputLayerNeurons = 4,
				MaxEras = 500,
				MinError = 0.0001,
				LearningRate = 0.2,
				Momentum = 0.4
			};
		}

		public static NetworkBuilder GetBuilder()
		{
			return new NetworkBuilder();
		}

		public NetworkBuilder SetInputLayerNeurons(int neurons)
		{
			if (neurons < 1)
				throw new ArgumentException("Layer should have at least one neuron.");

			networkConfiguration.InputLayerNeurons = neurons;

			return this;
		}

		public NetworkBuilder SetHiddenLayerNeurons(int neurons)
		{
			if (neurons < 1)
				throw new ArgumentException("Layer should have at least one neuron.");

			networkConfiguration.HiddenLayerNeurons = neurons;

			return this;
		}

		public NetworkBuilder SetOutputLayerNeurons(int neurons)
		{
			if (neurons < 1)
				throw new ArgumentException("Layer should have at least one neuron.");

			networkConfiguration.OutputLayerNeurons = neurons;

			return this;
		}

		public NetworkBuilder SetMaxEras(int maxEras)
		{
			if (maxEras < 1)
				throw new ArgumentException("Network should have at least one era to learn.");

			return this;
		}

		public NetworkBuilder SetMinError(double minError)
		{
			if (minError >= 1)
				throw new ArgumentException("Min error should be significantly lower than 1.");

			networkConfiguration.MinError = minError;

			return this;
		}

		public NetworkBuilder SetLearningRate(double learningRate)
		{
			if (learningRate >= 100d)
				throw new ArgumentException("Learning rate should be lower than 100.");

			networkConfiguration.LearningRate = learningRate;

			return this;
		}

		public NetworkBuilder SetMomentum(double momentum)
		{
			if (momentum >= 1d)
				throw new ArgumentException("Momentum should be lower than 1.");

			networkConfiguration.Momentum = momentum;

			return this;
		}

		public Network Build() => new Network(networkConfiguration);
	}
}
