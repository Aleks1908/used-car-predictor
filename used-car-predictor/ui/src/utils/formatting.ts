export const formatCarName = (
  manufacturer?: string,
  model?: string
): string => {
  if (!manufacturer || !model) return "";
  return `${manufacturer.charAt(0).toUpperCase()}${manufacturer.slice(
    1
  )} ${model.charAt(0).toUpperCase()}${model.slice(1)}`;
};
