export const formatCarName = (
  manufacturer?: string,
  model?: string
): string => {
  if (!manufacturer && !model) return "";

  const formattedManufacturer = manufacturer
    ? `${manufacturer.charAt(0).toUpperCase()}${manufacturer.slice(1)}`
    : "";

  const formattedModel = model
    ? `${model.charAt(0).toUpperCase()}${model.slice(1)}`
    : "";

  if (!formattedManufacturer) return formattedModel;
  if (!formattedModel) return formattedManufacturer;

  return `${formattedManufacturer} ${formattedModel}`;
};
